import atexit
import sys
from typing import TextIO, Optional

def register(message: str, *, stream: Optional[TextIO] = None) -> None:
    """Print `message` when the interpreter exits."""
    out = sys.stdout if stream is None else stream

    def _on_exit() -> None:
        try:
            print(message, file=out)
            out.flush()
        except Exception:
            pass

    atexit.register(_on_exit)
