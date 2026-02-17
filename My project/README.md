# Renderman

A top-down survival-collection game prototype built in Unity for **CSCI 426 — Prototype 04: Feedback and Balance**.

The player navigates a dark, fog-of-war world with extremely limited visibility, collecting pairs of glasses to expand their vision. But every improvement comes at a cost — a mysterious entity called **Renderman** stalks the edges of perception, repositioning more aggressively as the player's awareness grows. The player must balance the desire to see more of the world against the escalating danger that visibility brings.

---

## How to Play

- **WASD / Arrow Keys** — Move
- **Left Shift** — Sprint (drains stamina)
- **M** — Toggle map overlay
- **Escape** — Pause menu
- **Objective** — Collect all 5 pairs of glasses and survive
- **Lose condition** — Exposure meter fills completely

**Target play session:** 1–2 minutes.

---

## Core Mechanics

### Vision (Vignette)
The player starts with a very narrow circle of visibility created by a black vignette overlay. Each pair of glasses collected expands this radius by a 1.4x multiplier. As the vignette recedes, the world opens up — but so does the player's vulnerability.

### Renderman (The Threat)
A single persistent entity that spawns nearby and repositions on a timer. Every other spawn is **tailored** — placed within a 45-degree cone of the player's current movement direction, increasing the chance of an encounter. If any part of Renderman is visible within the player's vision circle (and on-screen), the **exposure meter** climbs. The vignette transitions from black to TV static as exposure rises, creating escalating visual discomfort that incentivizes the player to move away.

### Exposure Meter
When Renderman is visible, exposure increases. When not visible, it decays — but significantly slower than it builds. Reaching full exposure ends the run. The gain rate also increases with each pair of glasses collected, compounding the cost of expanded vision.

### Sprint and Stamina
Sprinting lets the player escape danger quickly but drains stamina. Stamina has a feedback-driven drain/regen system: drain accelerates as stamina empties, and regeneration accelerates as stamina fills. A world-space stamina bar hovers above the player.

### Lifesaver
A hidden collectible inflatable ring. Picking it up attaches it to the player and disables water collision, opening previously inaccessible areas of the map.

### Glasses Pickup Animation
Each glasses collection triggers a heal animation on the player character and a fade-in/fade-out notification at the top of the screen.

---

## Feedback Loops

### Stock: Vision

| Direction | Loop Type | Description |
|-----------|-----------|-------------|
| Vision increases | **Positive (reinforcing)** | Larger visible area makes it easier to spot the next pair of glasses, which further increases vision. |
| Vision increases | **Negative (balancing)** | More vision means Renderman's spawn radius is more likely to overlap the visible area. Exposure gain rate also increases per glasses collected. The player must spend more time dodging and less time exploring, slowing progress. |

### Stock: Stamina

| Direction | Loop Type | Description |
|-----------|-----------|-------------|
| Stamina decreases | **Positive (reinforcing)** | Lower stamina causes drain rate to accelerate (drain lerps from 20/s at full to 50/s at empty), making it deplete even faster. |
| Stamina increases | **Positive (reinforcing)** | Higher stamina causes regen rate to accelerate (regen lerps from 5/s at empty to 25/s at full), making it recover even faster. |
| Stamina depletes | **Negative (balancing)** | Hitting zero forces the player to stop sprinting entirely, which allows the regen delay to pass and stamina to begin recovering. The system self-corrects. |

### Stock: Exposure

| Direction | Loop Type | Description |
|-----------|-----------|-------------|
| Exposure increases | **Negative (balancing)** | Rising exposure causes the vignette to fill with TV static, creating strong visual feedback that the player is in danger. This naturally drives the player to move away from Renderman, which causes exposure to decay. The discomfort is the balancing force. |

---

## Balance Approach

The game sits in a narrow band of playability by layering reinforcing and balancing loops against each other:

- **Glasses collection** is simultaneously rewarding (more vision) and punishing (faster exposure gain, more aggressive Renderman timing). This prevents a pure snowball — each pickup makes the game harder, not just easier.
- **Stamina's dual positive loops** (fast drain at low, fast regen at high) create a natural rhythm of sprint-rest-sprint that the player must respect. The negative loop of forced rest prevents complete depletion spirals.
- **Exposure decay being slower than gain** means the player can't just briefly glance at Renderman and walk away unscathed — they need to proactively avoid line of sight, not reactively flee.
- **Tailored spawns** (every other Renderman appears in the player's movement direction) ensure that the threat isn't purely random — attentive players who vary their pathing are rewarded.

The result is a game where improving your position (more vision) simultaneously raises the stakes, creating tension that scales with progress rather than diminishing.

---

## Tech Stack

- **Engine:** Unity (URP, 2D)
- **Input:** Unity Input System (new)
- **Art:** Minifantasy Forgotten Plains tileset, Tiny Swords character pack
- **All game UI** is generated at runtime via code (no prefab canvases)
