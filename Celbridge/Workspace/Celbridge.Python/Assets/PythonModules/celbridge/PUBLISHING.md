# Build & Publish with `uv`

Assumes you have `uv` installed: https://docs.astral.sh/uv/

## 1) Build
```bash
uv build
```
Creates `dist/celbridge-0.1.0.tar.gz` (sdist) and `dist/celbridge-0.1.0-py3-none-any.whl` (wheel).

## 2) Test publish (TestPyPI)
Create a token on TestPyPI and then:
```bash
uv publish --repository testpypi --token pypi-***YOUR_TESTPYPI_TOKEN***
```

## 3) Publish to PyPI
Create a PyPI token and then:
```bash
uv publish --token pypi-***YOUR_PYPI_TOKEN***
```

> Alternative:
> ```bash
> uv publish --username __token__ --password pypi-***YOUR_PYPI_TOKEN***
> ```

## 4) Install & run
```bash
uv pip install celbridge
celbridge --version
```

## Notes
- This example has **no third‑party dependencies**; it only uses the Python standard library.
- The console script is declared in `[project.scripts]` as `celbridge = "celbridge.cli:main"`.
