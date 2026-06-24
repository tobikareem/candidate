# Convenience targets. Edit `reconcile` to run YOUR service.
.PHONY: reconcile selfcheck up clean

# Run your service end-to-end over data/ and write out/<study>/{chains,dashboard,unbilled,unpaid}.json.
# The default points at the Python skeleton; replace with your own entry point.
# `dotnet run` sets the app's cwd to the project dir, so --out anchors output at the repo root.
reconcile:
	dotnet run --project src/Recon.App -- --reconcile --out $(CURDIR)/out

# Validate your out/ structure and score it against the public sample.
selfcheck:
	python3 conformance/run_conformance.py --out ./out

# Bring up the optional Postgres + your app container.
up:
	docker compose up --build

clean:
	rm -rf out
