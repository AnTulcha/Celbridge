# celbridge

This is a minimal, PyPI-ready example of a package that exposes a console script
named **`celbridge`** using `[project.scripts]`.

It uses a *src layout* and Hatchling as the build backend.

## Quick start (local dev)

```bash
# from the project root
uv pip install -e .
celbridge --version
```

You can also run the package module directly:

```bash
python -m celbridge --version
```

## Build and publish with `uv`

> **Tip:** test on TestPyPI first.

```bash
# Build sdist and wheel into ./dist/
uv build

# Publish to TestPyPI
uv publish --repository testpypi --token pypi-***YOUR_TESTPYPI_TOKEN***

# (Later) Publish to PyPI
uv publish --token pypi-***YOUR_PYPI_TOKEN***
```

If you prefer username/password style for API tokens:

```bash
uv publish --username __token__ --password pypi-***YOUR_TOKEN***
```

## Entry point behavior

The console script `celbridge` calls `celbridge.cli:main()` which delegates to
`celbridge.cli:_startup()`. Replace `_startup()` with your real app startup.
