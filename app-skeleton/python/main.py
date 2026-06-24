#!/usr/bin/env python3
"""Minimal reconciliation-service skeleton (Python stdlib only).
Implement reconcile(): ingest DATA_DIR -> model -> write the four files per OUTPUT_CONTRACT.md.
"""
import json
import os
import sys
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

DATA = Path(os.environ.get("DATA_DIR", "documents"))  # the one flat pile of all documents
OUT = Path(os.environ.get("OUT_DIR", "out"))
STUDIES = ["study-01-horizon", "study-02-ascend", "study-03-northstar"]


def reconcile():
    """TODO: parse DATA, model entities, resolve the six chains, compute the products.
    Right now it writes empty stubs so the pipeline runs end-to-end."""
    for s in STUDIES:
        d = OUT / s
        d.mkdir(parents=True, exist_ok=True)
        (d / "chains.json").write_text(json.dumps({
            "study_id": s, "payment_to_remittance": [], "invoice_to_payment": [],
            "invoice_to_activities": [], "remittance_to_activities": [],
            "activity_to_cta": [], "entity_scope": []}, indent=2))
        (d / "dashboard.json").write_text(json.dumps({
            "study_id": s, "total_billed": 0, "total_collected": 0, "outstanding_ar": 0,
            "holdback_withheld": 0, "unbilled_estimate": 0, "exceptions_count": 0,
            "avg_days_to_payment": None}, indent=2))
        (d / "unbilled.json").write_text("[]")
        (d / "unpaid.json").write_text("[]")
    return {"status": "done", "studies": STUDIES, "out": str(OUT)}


class Handler(BaseHTTPRequestHandler):
    def _send(self, code, body):
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(body if isinstance(body, bytes) else json.dumps(body).encode())

    def do_GET(self):
        if self.path == "/healthz":
            return self._send(200, b"ok")
        if self.path.startswith("/reconcile"):
            return self._send(200, reconcile())
        # TODO: expose your chains here, e.g. GET /chains/<study>, /derived/unbilled/<study>, ...
        self._send(404, {"error": "not found"})

    def log_message(self, *a):
        pass


if __name__ == "__main__":
    if "--reconcile" in sys.argv:
        print(reconcile())
    else:
        print("serving on :8080")
        HTTPServer(("0.0.0.0", 8080), Handler).serve_forever()
