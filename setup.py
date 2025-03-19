#!/usr/bin/env python3
# -*- coding: utf-8 -*-

from setuptools import setup, find_packages

with open("README.md", "r", encoding="utf-8") as fh:
    long_description = fh.read()

setup(
    name="unity-mcp",
    version="1.0.0",
    author="UnityMCP Developers",
    author_email="info@unitymcp.com",
    description="Python client for communicating with Unity using the Model Control Protocol",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/your-username/UnityMCP",
    packages=find_packages(),
    classifiers=[
        "Development Status :: 4 - Beta",
        "Intended Audience :: Developers",
        "Topic :: Software Development :: Libraries :: Python Modules",
        "License :: OSI Approved :: MIT License",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.6",
        "Programming Language :: Python :: 3.7",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
    ],
    python_requires=">=3.6",
    install_requires=[
        "uuid",
    ],
    keywords="unity, game development, 3d, visualization, tcp",
) 