#!/usr/bin/env python3
"""Check serialized Unity prefab/scene PPtrs and component/hierarchy membership.

Requires PyYAML. This is a static source validator, not a Unity importer.
Usage: python3 validate_unity_assets.py /path/to/Projeto [--output report.json]
       python3 validate_unity_assets.py /path/to/Projeto --file path/to/old.prefab
"""
from pathlib import Path
import argparse
import collections
import json
import re
import sys
import yaml

HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)(?: stripped)?\s*$", re.M)


def references(value, path=""):
    if isinstance(value, dict):
        if "fileID" in value:
            yield path, value
        for key, item in value.items():
            yield from references(item, f"{path}/{key}")
    elif isinstance(value, list):
        for index, item in enumerate(value):
            yield from references(item, f"{path}/{index}")


def read_records(path):
    source = path.read_text(encoding="utf-8-sig")
    headers = list(HEADER.finditer(source))
    records = []
    for index, header in enumerate(headers):
        end = headers[index + 1].start() if index + 1 < len(headers) else len(source)
        document = yaml.safe_load(source[header.end():end])
        if not isinstance(document, dict) or len(document) != 1:
            raise ValueError(f"Invalid serialized document {header.group(2)}")
        typename, fields = next(iter(document.items()))
        records.append((int(header.group(2)), int(header.group(1)), typename, fields))
    return records


def audit(path, guidmap):
    errors = []
    records = read_records(path)
    ids = [row[0] for row in records]
    duplicates = [value for value, count in collections.Counter(ids).items() if count > 1]
    errors.extend(f"Duplicate document ID: {value}" for value in duplicates)
    by_id = {row[0]: row for row in records}
    source_guids = set()
    external_guids = set()
    local_count = 0
    for file_id, _, typename, fields in records:
        for prop, reference in references(fields):
            target = reference["fileID"]
            if "guid" in reference:
                guid = str(reference["guid"])
                if guid.startswith("0000000000000000"):
                    continue
                if guid in guidmap:
                    source_guids.add(guid)
                    if not guidmap[guid].exists():
                        errors.append(f"{file_id}{prop}: missing source asset {guidmap[guid]}")
                else:
                    external_guids.add(guid)
            elif target != 0:
                local_count += 1
                if target not in by_id:
                    errors.append(f"{file_id}{prop}: missing local fileID {target}")

    owners = {}
    for file_id, _, typename, fields in records:
        if typename != "GameObject":
            continue
        components = [entry["component"]["fileID"] for entry in fields.get("m_Component", [])]
        repeated = [value for value, count in collections.Counter(components).items() if count > 1]
        errors.extend(f"GameObject {file_id}: duplicate component {value}" for value in repeated)
        transforms = []
        for component in components:
            owners.setdefault(component, []).append(file_id)
            record = by_id.get(component)
            if record is None:
                continue
            if record[2] in ("Transform", "RectTransform"):
                transforms.append(component)
            if record[3].get("m_GameObject", {}).get("fileID") != file_id:
                errors.append(f"GameObject {file_id}: component {component} has wrong owner")
        if len(transforms) != 1:
            errors.append(f"GameObject {file_id}: expected one Transform, got {len(transforms)}")

    for file_id, _, typename, fields in records:
        owner = fields.get("m_GameObject", {}).get("fileID", 0)
        if owner and owners.get(file_id) != [owner]:
            errors.append(f"Component {file_id}: listed by {owners.get(file_id, [])}, owner is {owner}")
        if typename not in ("Transform", "RectTransform"):
            continue
        children = [child["fileID"] for child in fields.get("m_Children", [])]
        if len(children) != len(set(children)):
            errors.append(f"Transform {file_id}: duplicate children")
        for child in children:
            record = by_id.get(child)
            if record and (record[2] not in ("Transform", "RectTransform") or
                           record[3].get("m_Father", {}).get("fileID") != file_id):
                errors.append(f"Transform {file_id}: child {child} lacks matching parent")
        parent = fields.get("m_Father", {}).get("fileID", 0)
        record = by_id.get(parent)
        if parent and record:
            siblings = [child["fileID"] for child in record[3].get("m_Children", [])]
            if file_id not in siblings:
                errors.append(f"Transform {file_id}: parent {parent} does not list child")
        visited = {file_id}
        while parent in by_id:
            if parent in visited:
                errors.append(f"Transform {file_id}: parent cycle")
                break
            visited.add(parent)
            parent = by_id[parent][3].get("m_Father", {}).get("fileID", 0)
    return {
        "path": str(path), "documents": len(records),
        "gameobjects": sum(row[2] == "GameObject" for row in records),
        "local_nonzero_references": local_count,
        "source_guid_count": len(source_guids),
        "external_package_or_builtin_guids": sorted(external_guids),
        "errors": errors,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("project", type=Path)
    parser.add_argument("--file", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    guidmap = {}
    for meta in (args.project / "Assets").rglob("*.meta"):
        match = re.search(r"^guid: ([a-f0-9]{32})$", meta.read_text(errors="ignore"), re.M)
        if match:
            guidmap[match.group(1)] = meta.with_suffix("")
    paths = [args.file] if args.file else sorted(
        list((args.project / "Assets").rglob("*.prefab")) +
        list((args.project / "Assets").rglob("*.unity")))
    results = [audit(path, guidmap) for path in paths]
    report = {"scope": "Static YAML validation; not Unity import or Play Mode",
              "asset_count": len(results), "assets": results,
              "error_count": sum(len(result["errors"]) for result in results)}
    encoded = json.dumps(report, ensure_ascii=False, indent=2)
    if args.output:
        args.output.write_text(encoded + "\n")
    print(encoded)
    return 1 if report["error_count"] else 0


if __name__ == "__main__":
    sys.exit(main())
