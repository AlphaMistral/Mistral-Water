# Mistral Water Simualtion System

* WARNING: Do NOT directly use the system in your project. This repository is still under development. If you detect any error or have any suggestion please raise an issue or contact me at Mistral@weymire.com

### Why I created this Repo

* 1. Fully understand water simulation techniques and corresponding computer graphics conceptions and read more papers. 
* 2. Possibly turn all these shit into formal use. (Who knows? )

### Ocean Renderer

* Based on Tessendorf's Paper and Stockham FFT. 
* Supports whitecap based on Jacobian determinant. 
* No external textures needed. 
* Only tested on DirectX. 

### Pond/Lake Water Renderer

* Supports sinusoids and Gerstner Waves. 
* Depth-based color tint. 
* Reflection and refraction. 
* Edge-detection foams. 

### On the Fly

* Ocean Renderer with be divided into two separate parts: one for theory and one for production. 
* Ocean Renderer's PC and console version will support MRT to boost performance and reduce draw call. 
* Ocean Renderer will fully support Mobile devices. 
* Ocean Renderer will integrate water particles to allow water dynamics. 
* Ocean Renderer will support flow map. 
* Pond Water Renderer will support more custom effects. 