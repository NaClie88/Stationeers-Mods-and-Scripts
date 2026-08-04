#!/usr/bin/env python3
"""
Stationeers Progression Guide Generator
Reads worlds.json + starts.json + core_tiers.md, outputs one combined
markdown guide per world (containing both Normal and Brutal start variants,
with world-specific priority/radiator/cooling notes injected).
"""
import json
import os

DB_DIR = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = "/mnt/user-data/outputs/world_guides"

with open(os.path.join(DB_DIR, "worlds.json")) as f:
    worlds = json.load(f)
with open(os.path.join(DB_DIR, "starts.json")) as f:
    starts = json.load(f)
with open(os.path.join(DB_DIR, "core_tiers.md")) as f:
    core_tiers = f.read()

os.makedirs(OUT_DIR, exist_ok=True)


def mining_list_md(targets):
    lines = []
    for i, t in enumerate(targets, 1):
        lines.append(f"{i}. **{t['item']}** — {t['amount']} — {t['reason']}")
    return "\n".join(lines)


def render_world_guide(key, w):
    normal = starts["normal"]
    brutal = starts["brutal"]

    md = []
    md.append(f"# Stationeers Progression Guide: {w['display_name']}")
    md.append(f"*Generated from the Stationeers Progression Database. Hazard profile: {w['hazard_profile']}.*")
    md.append("\n---\n")

    # Assumptions / world snapshot
    md.append("## **WORLD SNAPSHOT**\n")
    md.append(f"- **Hazard profile:** {w['hazard_profile']}")
    md.append(f"- **Confirmed conditions:** {w['confirmed_conditions']}")
    md.append(f"- **Atmosphere:** {w['atmosphere']}")
    md.append(f"- **Temperature:** {w['temperature']}")
    md.append(f"- **Gravity:** {w['gravity']}")
    md.append(f"- **Ice available for mining:** {'Yes' if w['ice_available'] else 'No'}")
    md.append(f"- **Sources:** {', '.join(w['sources'])}")
    md.append("\n---\n")

    # Priority modification callout
    md.append("## **PRIORITY ORDER MODIFICATION FOR THIS WORLD**\n")
    if w["is_baseline"]:
        md.append(f"**{w['priority_mod']}**")
    else:
        md.append(f"**{w['priority_mod']}**\n")
        if w.get("cooling_note"):
            md.append(f"**Cooling:** {w['cooling_note']}\n")
        if w.get("heating_note"):
            md.append(f"**Heating:** {w['heating_note']}\n")
    md.append(f"\n**Radiator note:** {w['radiator_note']}")
    md.append(f"\n**Mining note:** {w['mining_note']}")
    md.append("\n\n---\n")

    # Normal start
    md.append(f"## **STARTING CONDITION: {normal['display_name']}**\n")
    md.append(f"{normal['description']}\n")
    md.append(f"**Shelter requires mining first?** {'Yes' if normal['shelter_needs_mining'] else 'No — crate-supplied kits cover it.'}\n")
    md.append("### Day 1 Mining List (this world)")
    md.append(mining_list_md(normal["mining_targets"]))
    if not w["ice_available"]:
        md.append(f"\n**World override:** {w['mining_note']}")
    md.append(f"\n\n*Sources: {', '.join(normal['sources'])}*")
    md.append("\n\n---\n")

    # Brutal start
    md.append(f"## **STARTING CONDITION: {brutal['display_name']}**\n")
    md.append(f"{brutal['description']}\n")
    md.append("**Confirmed facts about this start:**")
    for fact in brutal["confirmed_facts"]:
        md.append(f"- {fact}")
    md.append("\n### Day 1 Mining List (this world, Brutal)")
    md.append(mining_list_md(brutal["mining_targets"]))
    if not w["ice_available"]:
        md.append(f"\n**World override:** {w['mining_note']}")
    md.append(f"\n\n*Sources: {', '.join(brutal['sources'])}*")
    md.append("\n\n---\n")

    # Universal core (Tiers 0-3, unchanged across worlds)
    md.append("## **UNIVERSAL PROGRESSION FRAMEWORK (TIERS 0–3)**")
    md.append("*Identical across all worlds and both starting conditions — only the sequencing above this line changes per world.*\n")
    md.append(core_tiers)

    return "\n".join(md)


generated = []
for key, w in worlds.items():
    content = render_world_guide(key, w)
    path = os.path.join(OUT_DIR, f"{key}_guide.md")
    with open(path, "w") as f:
        f.write(content)
    generated.append(path)
    print(f"Generated: {path} ({len(content)} bytes)")

print(f"\nTotal guides generated: {len(generated)}")
