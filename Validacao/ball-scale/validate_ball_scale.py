#!/usr/bin/env python3
"""Validate the dedicated ball sprite size without running the Unity importer.

Requires Python 3 and PyYAML. The adjacent baseline.json contains source hashes
and the previously committed prefab. The sibling prefab-repair validator checks
scene/prefab PPtrs and component membership.
"""
import argparse
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import re
import struct
import yaml


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(value, description):
    if not value:
        raise AssertionError(description)


def png_size(path):
    data = path.read_bytes()
    require(data[:8] == b"\x89PNG\r\n\x1a\n", "PNG signature")
    require(data[12:16] == b"IHDR", "PNG IHDR")
    return struct.unpack(">II", data[16:24])


def load_prefab_checker(path):
    spec = importlib.util.spec_from_file_location("prefab_checker", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("project", type=Path)
    parser.add_argument("--output", type=Path, default=Path("report.json"))
    parser.add_argument("--baseline", type=Path,
                        default=Path(__file__).parent / "baseline.json")
    parser.add_argument("--prefab-checker", type=Path,
                        default=Path(__file__).parent.parent / "prefab-repair" /
                        "validate_unity_assets.py")
    args = parser.parse_args()
    project = args.project.resolve()
    baseline = json.loads(args.baseline.read_text())
    texture = project / "Assets/Pokemon/Arte/Bola_Base_Pokemon.png"
    old_texture = project / "Assets/Pokemon/Arte/Nucleo_Pokebola.png"
    prefab = project / "Assets/Pokemon/Prefabs/Bola_Pokemon.prefab"
    metadata_path = Path(str(texture) + ".meta")
    metadata = yaml.safe_load(metadata_path.read_text())
    old_metadata = yaml.safe_load(baseline["old_texture_meta"])
    importer = metadata["TextureImporter"]
    old_importer = old_metadata["TextureImporter"]
    sprite = importer["spriteSheet"]["sprites"][0]
    old_sprite = old_importer["spriteSheet"]["sprites"][0]
    guid = metadata["guid"]
    sprite_id = sprite["spriteID"]

    require(re.fullmatch(r"[0-9a-f]{32}", guid), "New texture GUID format")
    require(re.fullmatch(r"[0-9a-f]{32}", sprite_id), "New sprite ID format")
    require(guid != baseline["old_sprite_guid"], "Dedicated sprite has its own GUID")
    require(sprite_id != old_sprite["spriteID"], "Dedicated sprite has its own sprite ID")
    require(texture.read_bytes() == old_texture.read_bytes(), "PNG pixels/bytes preserved")
    width, height = png_size(texture)
    require((width, height) == (512, 512), "Texture dimensions")
    require(len(importer["spriteSheet"]["sprites"]) == 1, "Single explicit sprite slice")
    require(sprite["rect"] == old_sprite["rect"], "Crop preserved")
    require(sprite["rect"] == {"serializedVersion": 2, "x": 13, "y": 13,
                                "width": 486, "height": 486}, "Expected opaque crop")
    require(sprite["rect"]["x"] + sprite["rect"]["width"] <= width and
            sprite["rect"]["y"] + sprite["rect"]["height"] <= height, "Crop inside PNG")
    require(importer["spritePixelsToUnits"] == 486, "One unit per sprite diameter")
    require(sprite["name"] == "Bola_Base_Pokemon" and sprite["internalID"] == 21300000,
            "Stable internal sprite fileID")
    require(importer["spriteSheet"]["nameFileIdTable"] == {"Bola_Base_Pokemon": 21300000},
            "Importer name/fileID table")

    expected_meta = baseline["old_texture_meta"].replace(
        "guid: " + baseline["old_sprite_guid"], "guid: " + guid).replace(
        "spritePixelsToUnits: 100", "spritePixelsToUnits: 486").replace(
        "Nucleo_Pokebola", "Bola_Base_Pokemon").replace(
        "spriteID: " + old_sprite["spriteID"], "spriteID: " + sprite_id)
    require(metadata_path.read_text() == expected_meta,
            "Metadata changes restricted to GUID, name, sprite ID and PPU")

    old_ref = "m_Sprite: {fileID: 21300000, guid: " + baseline["old_sprite_guid"] + ", type: 3}"
    new_ref = "m_Sprite: {fileID: 21300000, guid: " + guid + ", type: 3}"
    require(baseline["original_prefab"].count(old_ref) == 1, "Single old ball sprite reference")
    require(prefab.read_text() == baseline["original_prefab"].replace(old_ref, new_ref),
            "Prefab differs only by the sprite GUID")
    for rel, expected in baseline["preserved_source_sha256"].items():
        require(sha(project / rel) == expected, "Preserved source: " + rel)

    guidmap = {}
    for path in (project / "Assets").rglob("*.meta"):
        match = re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(errors="ignore"), re.M)
        if match:
            value = match.group(1)
            require(value not in guidmap, "Unique source asset GUID: " + value)
            target = path.with_suffix("")
            require(target.exists(), "Source for metadata: " + str(path))
            guidmap[value] = target
    require(guidmap[guid] == texture, "Prefab sprite GUID resolves to dedicated PNG")
    checker = load_prefab_checker(args.prefab_checker)
    assets = sorted(list((project / "Assets").rglob("*.prefab")) +
                    list((project / "Assets").rglob("*.unity")))
    audits = [checker.audit(path, guidmap) for path in assets]
    require(all(not audit["errors"] for audit in audits), "All prefab/scene PPtrs and hierarchy")
    records = checker.read_records(prefab)
    root = next(row[3] for row in records
                if row[2] == "GameObject" and row[3]["m_Name"] == "Bola_Pokemon")
    root_id = next(row[0] for row in records if row[3] is root)
    transform = next(row[3] for row in records
                     if row[2] == "Transform" and row[3]["m_GameObject"]["fileID"] == root_id)
    collider = next(row[3] for row in records
                    if row[2] == "CircleCollider2D" and row[3]["m_GameObject"]["fileID"] == root_id)
    visual = next(row[3] for row in records
                  if row[2] == "MonoBehaviour" and
                  row[3].get("m_EditorClassIdentifier") == "Assembly-CSharp::BolaPokemonVisual")
    scale = transform["m_LocalScale"]
    require(scale == {"x": 0.65, "y": 0.65, "z": 1}, "Root scale unchanged")
    require(collider["m_Radius"] == 0.5, "Collider radius unchanged")
    require(visual["ocupacaoRetrato"] == 0.88, "Portrait occupation unchanged")
    old_diameter = old_sprite["rect"]["width"] / old_importer["spritePixelsToUnits"] * scale["x"]
    new_diameter = sprite["rect"]["width"] / importer["spritePixelsToUnits"] * scale["x"]
    physical_diameter = 2 * collider["m_Radius"] * scale["x"]
    require(math.isclose(old_diameter, 3.159), "Previous oversized diameter reproduced")
    require(math.isclose(new_diameter, physical_diameter), "Visual matches physical diameter")
    report = {
        "baseline_commit": baseline["source_commit"],
        "source_asset": texture.relative_to(project).as_posix(),
        "texture_guid": guid, "sprite_id": sprite_id, "sprite_file_id": 21300000,
        "png_sha256": sha(texture), "png_bytes_identical_to_original": True,
        "texture_pixels": [width, height], "sprite_crop_pixels": [486, 486],
        "old_pixels_per_unit": old_importer["spritePixelsToUnits"],
        "new_pixels_per_unit": importer["spritePixelsToUnits"],
        "root_scale": scale, "collider_local_radius": collider["m_Radius"],
        "old_world_diameter": old_diameter, "old_rim_world_diameter": old_diameter * 1.1,
        "new_world_diameter": new_diameter, "new_rim_world_diameter": new_diameter * 1.1,
        "new_max_portrait_world_size": new_diameter * visual["ocupacaoRetrato"],
        "physical_world_diameter": physical_diameter,
        "prefab_change_only_sprite_guid": True,
        "preserved_source_files": len(baseline["preserved_source_sha256"]),
        "source_asset_guids_unique": len(guidmap),
        "prefab_scene_audits": audits,
        "unity_editor_executed": False,
        "limits": "Static importer metadata, PNG bytes, source dimensions and YAML references. "
                  "Does not run Unity import, rendering, callbacks or real physics."
    }
    args.output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n")
    print("PASS dedicated ball diameter 0.65, rim 0.715 and portrait maximum 0.572 world units")
    print("PASS original PNG bytes; prefab-only GUID change; %d preserved source hashes" %
          report["preserved_source_files"])
    print("PASS %d scene/prefab files without PPtr/component/hierarchy errors" % len(audits))
    print("Scope: static source validation; Unity Editor was not executed.")


if __name__ == "__main__":
    main()
