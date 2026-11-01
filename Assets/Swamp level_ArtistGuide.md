# Swamp level — Procreate Art & Layer Specification Guide

* **Target Game Resolution:** 3840 x 2160 px
* **Quality / Density Factor:** 1.00x
* **Painting Order:** Listed below from **Back to Front** (farthest background to closest foreground).

---

## Quick Summary Table (Back-to-Front)

| Order | Asset / Layer Name | Type | Depth (Z) | Procreate Canvas Size (W x H px) | Notes |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | **Swamp level background_0** | PaintedLayer | 3000.0 | **4158 x 2249 px** | - |
| 2 | **swamp tree_2** | ReusableProp | 100.0 | **9200 x 2026 px** | - |

---

## Per-Asset Templates & Details

### 1. Swamp level background_0
* **Role:** PaintedLayer
* **Scene Depth (Z):** `3000.00` (Sorting Order: `-3000`)
* **Recommended Procreate Canvas Size:** **4158 x 2249 px**
* **On-Screen Size Range:** 4134x2236 px (farthest zoom) up to 4158x2249 px (closest zoom)
* **Unity Import Settings (for Developer):** Set Transform Scale to `(1, 1, 1)` and Sprite PPU to `0.98` (World size: `4225.00 x 2285.27` units)
* **Warnings:** ⚠️ Undersized sprite (2048x1055 px < recommended 4158x2249 px); Exceeds max texture size threshold (4096 px)

### 2. swamp tree_2
* **Role:** ReusableProp
* **Scene Depth (Z):** `100.00` (Sorting Order: `-100`)
* **Recommended Procreate Canvas Size:** **9200 x 2026 px**
* **On-Screen Size Range:** 7987x1759 px (farthest zoom) up to 9199x2026 px (closest zoom)
* **Unity Import Settings (for Developer):** Set Transform Scale to `(1, 1, 1)` and Sprite PPU to `25.75` (World size: `357.30 x 78.67` units)
* **Warnings:** ⚠️ Undersized sprite (1830x403 px < recommended 9200x2026 px); Exceeds max texture size threshold (4096 px)

