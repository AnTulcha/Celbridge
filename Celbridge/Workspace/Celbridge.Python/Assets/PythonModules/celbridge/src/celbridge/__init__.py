# SPDX-License-Identifier: MIT

from .customize_ipython import apply_ipython_customizations
from .exit_lock import apply_exit_lock

__version__ = "0.1.0"
__all__ = ["apply_ipython_customizations", "apply_exit_lock"]
