"""Client minimo dell'API locale di ComfyUI (M13, D15), solo libreria standard.

Un workflow è un file in formato API in workflows/: ogni nodo ha un titolo (`_meta.title`) e il client
cambia gli ingressi per titolo, così i numeri dei nodi possono cambiare senza toccare il resto.
"""
import copy
import json
import os
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

HERE = os.path.dirname(os.path.abspath(__file__))
WORKFLOWS = os.path.join(HERE, "workflows")
DEFAULT_HOST = "127.0.0.1:8188"
# gli ingressi dei nodi che caricano un file dalla cartella models di ComfyUI
MODEL_INPUTS = ("ckpt_name", "clip_name", "unet_name", "vae_name", "bg_removal_name")


class ComfyError(Exception):
    pass


def load_workflow(name):
    with open(os.path.join(WORKFLOWS, name + ".json"), encoding="utf-8") as f:
        return json.load(f)


def node_by_title(workflow, title):
    found = [n for n in workflow.values() if n.get("_meta", {}).get("title") == title]
    if len(found) != 1:
        raise ComfyError(f"nel workflow ci sono {len(found)} nodi con il titolo '{title}'")
    return found[0]


def with_inputs(workflow, values):
    """Una copia del workflow con gli ingressi cambiati: values è {(titolo, ingresso): valore}."""
    workflow = copy.deepcopy(workflow)
    for (title, key), value in values.items():
        node = node_by_title(workflow, title)
        if key not in node["inputs"]:
            raise ComfyError(f"il nodo '{title}' non ha l'ingresso '{key}'")
        node["inputs"][key] = value
    return workflow


def required_models(workflow):
    """[(classe del nodo, ingresso, file)] per ogni modello che il workflow carica."""
    return [(n["class_type"], key, n["inputs"][key]) for n in workflow.values()
            for key in MODEL_INPUTS if key in n["inputs"]]


def _options(spec):
    """Le scelte di un ingresso a tendina, nei due formati di /object_info."""
    if isinstance(spec, list) and spec:
        if isinstance(spec[0], list):
            return spec[0]
        if spec[0] == "COMBO" and len(spec) > 1 and isinstance(spec[1], dict):
            return spec[1].get("options", [])
    return None


class Comfy:
    def __init__(self, host=DEFAULT_HOST, poll=1.0):
        self.base = "http://" + host
        self.poll = poll
        self.client_id = uuid.uuid4().hex

    # ---------- HTTP ----------

    def _request(self, path, data=None, headers=None, timeout=30):
        req = urllib.request.Request(self.base + path, data=data, headers=headers or {})
        try:
            with urllib.request.urlopen(req, timeout=timeout) as resp:
                return resp.read()
        except urllib.error.HTTPError as e:
            raise ComfyError(f"{path}: HTTP {e.code} {e.read().decode('utf-8', 'replace')[:2000]}") from None
        except urllib.error.URLError as e:
            raise ComfyError(f"ComfyUI non risponde su {self.base} ({e.reason}): l'app è aperta?") from None

    def _json(self, path, payload=None):
        data = None if payload is None else json.dumps(payload).encode("utf-8")
        headers = {} if payload is None else {"Content-Type": "application/json"}
        return json.loads(self._request(path, data, headers))

    # ---------- controlli ----------

    def version(self):
        return self._json("/system_stats")["system"]["comfyui_version"]

    def missing_models(self, workflow):
        """I file che il workflow carica ma che ComfyUI non vede nella cartella models."""
        missing = []
        for cls, key, name in required_models(workflow):
            info = self._json("/object_info/" + urllib.parse.quote(cls)).get(cls)
            if info is None:
                missing.append(f"{cls}: nodo sconosciuto in questa versione di ComfyUI")
                continue
            spec = {**info["input"].get("required", {}), **info["input"].get("optional", {})}.get(key)
            options = _options(spec)
            if options is not None and name not in options:
                missing.append(f"{name} ({cls}.{key})")
        return missing

    # ---------- esecuzione ----------

    def upload_image(self, path):
        """Carica un'immagine nella cartella input di ComfyUI e restituisce il nome da usare in LoadImage."""
        boundary = uuid.uuid4().hex
        with open(path, "rb") as f:
            content = f.read()
        name = os.path.basename(path)
        body = b"".join([
            f"--{boundary}\r\n".encode(),
            f'Content-Disposition: form-data; name="image"; filename="{name}"\r\n'.encode(),
            b"Content-Type: image/png\r\n\r\n", content, b"\r\n",
            f"--{boundary}\r\n".encode(),
            b'Content-Disposition: form-data; name="overwrite"\r\n\r\ntrue\r\n',
            f"--{boundary}--\r\n".encode(),
        ])
        reply = json.loads(self._request("/upload/image", body,
                                         {"Content-Type": f"multipart/form-data; boundary={boundary}"}))
        return reply["name"] if not reply.get("subfolder") else reply["subfolder"] + "/" + reply["name"]

    def run(self, workflow, timeout=1800):
        """Mette il workflow in coda, aspetta la fine e restituisce le uscite dei nodi ({id: {...}})."""
        reply = self._json("/prompt", {"prompt": workflow, "client_id": self.client_id})
        if reply.get("node_errors"):
            raise ComfyError("workflow rifiutato: " + json.dumps(reply["node_errors"], ensure_ascii=False)[:2000])
        prompt_id = reply["prompt_id"]
        start = time.monotonic()
        while time.monotonic() - start < timeout:
            history = self._json("/history/" + prompt_id).get(prompt_id)
            if history and history.get("status", {}).get("completed") is not None:
                status = history["status"]
                if status.get("status_str") == "error":
                    errors = [m[1] for m in status.get("messages", []) if m[0] == "execution_error"]
                    detail = "; ".join(f"{e.get('node_type')}: {e.get('exception_message', '').strip()}"
                                       for e in errors)
                    raise ComfyError("esecuzione fallita: " + (detail or json.dumps(status)[:2000]))
                return history["outputs"]
            time.sleep(self.poll)
        raise ComfyError(f"nessun risultato dopo {timeout} s (prompt {prompt_id})")

    def fetch(self, item):
        """Il contenuto di un file d'uscita ({filename, subfolder, type})."""
        query = urllib.parse.urlencode({"filename": item["filename"], "subfolder": item.get("subfolder", ""),
                                        "type": item.get("type", "output")})
        return self._request("/view?" + query, timeout=300)


def output_files(outputs, kind):
    """I file d'uscita di un tipo ("audio", "3d", "images") in tutti i nodi."""
    return [item for node in outputs.values() for item in node.get(kind, [])]
