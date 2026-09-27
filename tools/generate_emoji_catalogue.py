#!/usr/bin/env python3
"""Generate src/NiceMail/Resources/emoji.tsv, the emoji catalogue embedded in
the Windows build.

.NET has no equivalent of Python's unicodedata.name(), so the catalogue is
produced here, at development time, using the same id derivation as the
original GTK app (snake_case of the Unicode character name, e.g.
"WAVING HAND SIGN" -> "waving_hand_sign"). That keeps favourite_emoji.csv
compatible between the two front-ends.

Usage:
    python3 tools/generate_emoji_catalogue.py [emoji-data.txt]

If the Unicode emoji-data.txt file is supplied (from
https://www.unicode.org/Public/UCD/latest/ucd/emoji/emoji-data.txt), the
Emoji, Emoji_Presentation and Emoji_Modifier_Base properties are read from
it. Otherwise a built-in table (Unicode 15.0) is used. Either way, only
characters that this Python's unicodedata can name are emitted.

Output columns (tab-separated, UTF-8, no header):
    id  codepoints(hex, space-separated)  name  modifier_base(0|1)
"""
import sys
import unicodedata
from pathlib import Path

OUT = Path(__file__).resolve().parent.parent / "src" / "NiceMail" / "Resources" / "emoji.tsv"

BUILTIN_EMOJI = """
00A9 00AE 203C 2049 2122 2139 2194-2199 21A9-21AA 231A-231B 2328 23CF 23E9-23F3
23F8-23FA 24C2 25AA-25AB 25B6 25C0 25FB-25FE 2600-2604 260E 2611 2614-2615 2618
261D 2620 2622-2623 2626 262A 262E-262F 2638-263A 2640 2642 2648-2653 265F-2660
2663 2665-2666 2668 267B 267E-267F 2692-2697 2699 269B-269C 26A0-26A1 26A7
26AA-26AB 26B0-26B1 26BD-26BE 26C4-26C5 26C8 26CE-26CF 26D1 26D3-26D4 26E9-26EA
26F0-26F5 26F7-26FA 26FD 2702 2705 2708-270D 270F 2712 2714 2716 271D 2721 2728
2733-2734 2744 2747 274C 274E 2753-2755 2757 2763-2764 2795-2797 27A1 27B0 27BF
2934-2935 2B05-2B07 2B1B-2B1C 2B50 2B55 3030 303D 3297 3299
1F004 1F0CF 1F170-1F171 1F17E-1F17F 1F18E 1F191-1F19A 1F201-1F202 1F21A 1F22F
1F232-1F23A 1F250-1F251 1F300-1F321 1F324-1F393 1F396-1F397 1F399-1F39B
1F39E-1F3F0 1F3F3-1F3F5 1F3F7-1F4FD 1F4FF-1F53D 1F549-1F54E 1F550-1F567
1F56F-1F570 1F573-1F57A 1F587 1F58A-1F58D 1F590 1F595-1F596 1F5A4-1F5A5 1F5A8
1F5B1-1F5B2 1F5BC 1F5C2-1F5C4 1F5D1-1F5D3 1F5DC-1F5DE 1F5E1 1F5E3 1F5E8 1F5EF
1F5F3 1F5FA-1F64F 1F680-1F6C5 1F6CB-1F6D2 1F6D5-1F6D7 1F6DC-1F6E5 1F6E9
1F6EB-1F6EC 1F6F0 1F6F3-1F6FC 1F7E0-1F7EB 1F7F0 1F90C-1F93A 1F93C-1F945
1F947-1F9FF 1FA70-1FA7C 1FA80-1FA88 1FA90-1FABD 1FABF-1FAC5 1FACE-1FADB
1FAE0-1FAE8 1FAF0-1FAF8
"""

# Emoji that default to text presentation; these get U+FE0F appended.
BUILTIN_TEXT_DEFAULT = """
00A9 00AE 203C 2049 2122 2139 2194-2199 21A9-21AA 2328 23CF 23ED-23EF 23F1-23F2
23F8-23FA 24C2 25AA-25AB 25B6 25C0 25FB-25FC 2600-2604 260E 2611 2618 261D 2620
2622-2623 2626 262A 262E-262F 2638-263A 2640 2642 265F-2660 2663 2665-2666 2668
267B 267E 2692 2694-2697 2699 269B-269C 26A0 26A7 26B0-26B1 26C8 26CF 26D1 26D3
26E9 26F0-26F1 26F4 26F7-26F9 2702 2708-2709 270C-270D 270F 2712 2714 2716 271D
2721 2733-2734 2744 2747 2763-2764 27A1 2934-2935 2B05-2B07 3030 303D 3297 3299
1F170-1F171 1F17E-1F17F 1F202 1F237 1F321 1F324-1F32C 1F336 1F37D 1F396-1F397
1F399-1F39B 1F39E-1F39F 1F3CB-1F3CE 1F3D4-1F3DF 1F3F3 1F3F5 1F3F7 1F43F 1F441
1F4FD 1F549-1F54A 1F56F-1F570 1F573-1F579 1F587 1F58A-1F58D 1F590 1F5A5 1F5A8
1F5B1-1F5B2 1F5BC 1F5C2-1F5C4 1F5D1-1F5D3 1F5DC-1F5DE 1F5E1 1F5E3 1F5E8 1F5EF
1F5F3 1F5FA 1F6CB 1F6CD-1F6CF 1F6E0-1F6E5 1F6E9 1F6F0 1F6F3
"""

BUILTIN_MODIFIER_BASE = """
261D 26F9 270A-270D 1F385 1F3C2-1F3C4 1F3C7 1F3CA-1F3CC 1F442-1F443 1F446-1F450
1F466-1F478 1F47C 1F481-1F483 1F485-1F487 1F48F 1F491 1F4AA 1F574-1F575 1F57A
1F590 1F595-1F596 1F645-1F647 1F64B-1F64F 1F6A3 1F6B4-1F6B6 1F6C0 1F6CC 1F90C
1F90F 1F918-1F91F 1F926 1F930-1F939 1F93C-1F93E 1F977 1F9B5-1F9B6 1F9B8-1F9B9
1F9BB 1F9CD-1F9CF 1F9D1-1F9DD 1FAC3-1FAC5 1FAF0-1FAF8
"""

# Components that are not useful on their own in a picker.
EXCLUDE = set(range(0x1F3FB, 0x1F400)) | set(range(0x1F9B0, 0x1F9B4)) | set(range(0x1F1E6, 0x1F200))


def parse_ranges(text):
    out = set()
    for tok in text.split():
        if "-" in tok:
            a, b = tok.split("-")
            out.update(range(int(a, 16), int(b, 16) + 1))
        else:
            out.add(int(tok, 16))
    return out


def parse_emoji_data(path):
    props = {"Emoji": set(), "Emoji_Presentation": set(), "Emoji_Modifier_Base": set()}
    for line in Path(path).read_text(encoding="utf-8").splitlines():
        line = line.split("#", 1)[0].strip()
        if not line:
            continue
        cps, prop = [p.strip() for p in line.split(";")]
        if prop not in props:
            continue
        if ".." in cps:
            a, b = cps.split("..")
            props[prop].update(range(int(a, 16), int(b, 16) + 1))
        else:
            props[prop].add(int(cps, 16))
    emoji = {c for c in props["Emoji"] if c > 0x7F}  # drop digits, #, *
    text_default = emoji - props["Emoji_Presentation"]
    return emoji, text_default, props["Emoji_Modifier_Base"]


def snake(name):
    out, prev_us = [], False
    for ch in name.lower():
        if ch.isalnum():
            out.append(ch)
            prev_us = False
        elif not prev_us:
            out.append("_")
            prev_us = True
    return "".join(out).strip("_")


def main():
    if len(sys.argv) > 1:
        emoji, text_default, mod_base = parse_emoji_data(sys.argv[1])
    else:
        emoji = parse_ranges(BUILTIN_EMOJI)
        text_default = parse_ranges(BUILTIN_TEXT_DEFAULT)
        mod_base = parse_ranges(BUILTIN_MODIFIER_BASE)
    emoji -= EXCLUDE

    rows, seen = [], set()
    for cp in sorted(emoji):
        name = unicodedata.name(chr(cp), None)
        if not name:
            continue  # unassigned in this Python's Unicode database
        ident = snake(name)
        if ident in seen:
            continue
        seen.add(ident)
        seq = [cp] + ([0xFE0F] if cp in text_default else [])
        rows.append("\t".join([
            ident,
            " ".join(f"{c:X}" for c in seq),
            name.capitalize(),
            "1" if cp in mod_base else "0",
        ]))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text("\n".join(rows) + "\n", encoding="utf-8")
    print(f"Wrote {len(rows)} emoji to {OUT} (Unicode {unicodedata.unidata_version})")


if __name__ == "__main__":
    main()
