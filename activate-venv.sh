#!/bin/bash
# Activation script for Python virtual environment
# Usage: source activate-venv.sh

if [ -f ".venv/bin/activate" ]; then
    source .venv/bin/activate
    echo "Python virtual environment activated!"
    echo "Python path: $(which python)"
    echo "Python version: $(python --version)"
else
    echo "Virtual environment not found. Creating one..."
    python3 -m venv .venv
    source .venv/bin/activate
    pip install -r requirements.txt
    echo "Virtual environment created and activated!"
fi