# YAPP2

YAPP2 is a backward-compatible superset of the packet JSON produced by
[YetAnotherPacketParser](https://github.com/alopezlago/YetAnotherPacketParser) (YAPP)
and consumed by the MODAQ reader.

It exists to carry things plain YAPP cannot:

- **which words a pronunciation guide covers** (1.0),
- **the order a packet is read in** when tossups and bonuses interlace (1.1), and
- **the rules of the game the packet is written for** — regulation length, power markers
  and their values, neg value, and so on — so a reader doesn't have to set the game up
  by hand each time (1.2).

Version: **`yapp2/1.2`**.

## The problem

A pronunciation guide is a parenthesized respelling:

> Denis Diderot ("DID-er-OW") edited the *Encyclopédie*.

Plain YAPP stores that as literal text, which is enough to *display* but not enough to
know that the guide belongs to "Diderot" rather than to "Denis", to both words, or to
the sentence. Authoring tools do track it — QEMS marks the covered words with `\P`
markers and styles them — and every export to YAPP had to discard it.

## Design

Two rules, in priority order:

1. **An existing YAPP consumer must read a YAPP2 file correctly, with no changes.**
2. A YAPP2-aware consumer gets the anchoring.

That rules out simply putting a new tag in `question`: MODAQ's text parser strips only
the tags it knows, so an unrecognized `<pg>` would render as the literal characters
`<pg>` in every existing reader. So the canonical fields stay exactly as plain YAPP
would have them, and the anchored variants live beside them.

## Shape

```json
{
  "version": "yapp2/1.2",
  "name": "Round 1",
  "gameFormat": {
    "displayName": "mACF with powers",
    "regulationTossupCount": 20,
    "powers": [{"marker": "(*)", "points": 15}],
    "negValue": -5,
    "bonusesBounceBack": false
  },
  "readingOrder": [
    {"type": "tossup", "index": 0},
    {"type": "bonus", "index": 0}
  ],
  "tossups": [
    {
      "number": 1,
      "question": "Denis Diderot (\"DID-er-OW\") edited this work.",
      "answer": "<b><u>Encyclopédie</u></b>",
      "metadata": "A Writer, Literature - European",
      "anchored": {
        "question": "Denis <pg>Diderot</pg> (\"DID-er-OW\") edited this work."
      }
    }
  ],
  "bonuses": [
    {
      "number": 1,
      "leadin": "...",
      "parts": ["...", "..."],
      "answers": ["...", "..."],
      "values": [10, 10],
      "anchored": {
        "parts": ["A <pg>part</pg> (\"PART\") here.", "No anchor in this one."]
      }
    }
  ]
}
```

Everything except `version`, `gameFormat`, `readingOrder` and `anchored` is unchanged
from YAPP.

### `version`

A string, `"yapp2/<major>.<minor>"`. Its absence means the file is plain YAPP, and a
reader must then ignore `anchored`, `readingOrder` and `gameFormat` entirely — a file that omits the
marker has not promised that its canonical fields are tag-free, so trusting `anchored`
there risks rendering a tag the file never declared.

Compare case-insensitively on the `yapp2/` prefix. A reader that understands
`yapp2/1.0` should accept later `1.x` minor versions, since minor bumps only add
optional fields.

### `gameFormat`

*Added in 1.2.* Optional, top level. The rules of the game the packet is written for, so
a reader can set the game up from the packet instead of the moderator customizing it
every time.

```json
"gameFormat": {
  "displayName": "mACF with powers",
  "regulationTossupCount": 20,
  "minimumOvertimeQuestionCount": 1,
  "overtimeIncludesBonuses": false,
  "powers": [{"marker": "(+)", "points": 20}, {"marker": "(*)", "points": 15}],
  "negValue": -5,
  "bonusesBounceBack": false,
  "timeoutsAllowed": 1,
  "tossupsOnly": false,
  "pronunciationGuideMarkers": ["(\"", "\")"]
}
```

| Key | Type | Meaning |
|---|---|---|
| `displayName` | string | The format's name, such as `"ACF"` or `"PACE"`. |
| `regulationTossupCount` | int | Tossups in regulation, not counting tiebreakers. |
| `minimumOvertimeQuestionCount` | int | The fewest tossups read in overtime before a tied game can end. |
| `overtimeIncludesBonuses` | bool | Whether an overtime tossup earns a bonus. |
| `powers` | array | Power markers and what a correct buzz before each is worth (below). |
| `negValue` | int | Points for an incorrect interrupt, e.g. `-5`. `0` means no negs. |
| `bonusesBounceBack` | bool | Whether the other team can answer bonus parts the controlling team misses. |
| `timeoutsAllowed` | int | Timeouts per team. |
| `tossupsOnly` | bool | Whether the format has no bonuses. |
| `pronunciationGuideMarkers` | [string, string] | The text that opens and closes a guide, e.g. `["(\"", "\")"]`. |

The key names are MODAQ's game format names, so a reader built on MODAQ can apply the
object directly.

Rules:

- **Every key is optional.** A key that is present sets that rule; a key that is absent
  leaves it to the reader, which uses whatever it would have used for a plain YAPP
  packet. A producer writes only what it actually knows.
- **It is a default, not a lock.** A reader should start the game with these rules but
  still let the moderator change them.
- **A bad key is ignored on its own.** A key with the wrong type or an impossible value
  (a negative tossup count, a guide marker array that isn't two strings) is dropped and
  the rest of the object still applies. Readers also ignore keys they don't know, so a
  later minor version can add rules.
- **Each packet carries its own.** A reader loading several packets at once can use the
  first one's format, or warn when they disagree.

#### `powers`

An array of `{"marker": <string>, "points": <int>}`, **highest `points` first**.

- `marker` is the literal text in the tossup's canonical `question` that ends that power
  region, such as `(*)` or `(+)`. A correct buzz is worth the `points` of the first
  marker after the buzzed word, so with superpowers — `(+)` then `(*)` — a buzz before
  `(+)` earns its points and a buzz between the two earns `(*)`'s. After the last marker
  a tossup is worth its normal value.
- An **empty array** means the format has no powers. That is different from leaving
  `powers` out, which means the packet doesn't say.
- A marker that never appears in a tossup simply never applies to that tossup.

Powers are identified by marker rather than by word position for the same reason
anchors travel inside the text: the marker is already in the question, so the two
can't fall out of sync, while a word index silently goes stale when anyone edits the
question.

### `readingOrder`

*Added in 1.1.* Optional, top level. The order the packet's questions are read, as an
array of `{"type": "tossup" | "bonus", "index": <int>}` entries. `index` is a 0-based
position in the canonical `tossups` / `bonuses` array.

A packet whose tossups and bonuses interlace — tossup 1, bonus 1, tossup 2, bonus 2 —
is written as:

```json
"readingOrder": [
  {"type": "tossup", "index": 0},
  {"type": "bonus", "index": 0},
  {"type": "tossup", "index": 1},
  {"type": "bonus", "index": 1}
]
```

Rules:

- **It reorders; it never adds, removes or edits.** Every question in `tossups` and
  `bonuses` must appear exactly once. This is what keeps rule 1 intact: a reader that
  ignores the field still gets the entire packet, just grouped the plain-YAPP way.
- Omit it entirely for the ordinary all-tossups-then-all-bonuses packet. Its absence is
  not "unknown order" — it means exactly that default.
- A reader that finds it malformed — an index out of range, a duplicate, or any question
  left out — must **discard the whole field** and fall back to the default order. A
  partially-applied order would drop or repeat questions mid-packet, which is worse than
  ignoring the hint.
- It says nothing about numbering. Question numbers stay in each question's `number`
  field, and interlacing does not renumber anything.

Why an explicit list rather than an `"interlaced": true` flag: real packets aren't
always strictly alternating (tiebreakers at the end, a bonus-less lightning round), and
a list describes any arrangement while a boolean describes one. The alternating case is
just the common shape of it.

### `anchored`

Optional, per question. Holds the same text as the canonical fields with `<pg>` tags
added, and **only** the fields that actually contain an anchor:

| Question type | Allowed keys |
|---|---|
| tossup | `question`, `answer` |
| bonus | `leadin`, `parts`, `answers` |

Rules:

- Omit `anchored` entirely when no field on that question has an anchor. Most questions
  have none, so a YAPP2 file is barely larger than the YAPP equivalent.
- Omit individual keys with no anchor rather than repeating the canonical text.
- `parts` and `answers` are **full-length** arrays matching the canonical arrays
  element-for-element, because they are matched up by index. A reader that sees a
  length mismatch must ignore that array rather than pair strings up wrongly.
- Apart from `<pg>` tags, an anchored string must be identical to its canonical
  counterpart. Anything else (a different answer, extra words) is malformed.

### The `<pg>` tag

`<pg>…</pg>` wraps the word(s) a nearby pronunciation guide covers.

```
Denis <pg>Diderot</pg> ("DID-er-OW") edited this work.
```

- It may contain the inline tags YAPP already allows (`<b> <u> <em> <sup> <sub>`), and
  may appear inside them.
- It does not nest inside itself.
- It carries no attributes.
- It is not tied to a specific guide by ID. The convention is that the anchor precedes
  its guide; a reader that wants to pair them can take the next guide after the anchor.

**The anchored words are ordinary question words.** They are read aloud, they count
toward word counts, and they are buzzable. This is the one thing an implementer is most
likely to get wrong, because the *guide itself* is the opposite on all three counts. A
reader that treats an anchor like a guide will shift every buzz position after it.

`<pg>` is presentational metadata: a consumer that does not care may strip the tags and
lose nothing but the anchoring.

## Producing YAPP2

Emit both forms of a field, then drop the anchored one if it came out identical. A
producer with nothing YAPP2 to say — no anchors, no `readingOrder` and no `gameFormat` —
should emit plain YAPP (no `version`) rather than an empty YAPP2 file.

Write `gameFormat` only with the rules the producer actually knows, sort `powers`
highest first, and use the marker exactly as it appears in the questions.

Write `readingOrder` only when the packet really is read out of the default order.
Emitting the default order explicitly is legal but pointless, and it invites a reader to
treat "no `readingOrder`" as a different case from "the default `readingOrder`".

**All-power tossups.** A producer that records "the whole stem is power" as a flag rather
than a marker has to write the marker out anyway: YAPP and YAPP2 both locate the power
boundary from the literal `(*)` in the question, so a stem without one scores no power at
all. Put the marker after the last word — everything before it is power, which is the
whole question. (QEMS does this; see `all_power_tail` in its `yapp_export`.)

## Consuming YAPP2

```
if version starts with "yapp2/":
    if gameFormat is present:
        start from the reader's default game format
        apply each gameFormat key that is well-formed; ignore the rest
        let the moderator still change any of it
    for each question:
        for each field:
            use anchored[field] if present (and, for arrays, the same length)
            else use the canonical field
    if readingOrder is present and covers every question exactly once:
        present the questions in that order
    else:
        present all tossups, then all bonuses
else:
    read as plain YAPP; ignore anchored, readingOrder and gameFormat
```

Because the two forms differ only by `<pg>`, a reader that understands the tag loses
nothing by always preferring the anchored text.

## Implementations

- **YetAnotherPacketParser** (producer): `-f yapp2` on the command line, `?format=yapp2` on the API, or
  `Yapp2Format` on `JsonPacketCompilerOptions`. Anchors come from colored text that a guide immediately
  follows in a .docx file (`Lexer/PronunciationAnchorFilter.cs`) or from `<pg>` tags in an HTML file
  (`Lexer/HtmlLexer.cs`); `Compiler/Json/JsonAnchoredTossupNode.cs` and `JsonAnchoredBonusNode.cs` write the
  `anchored` objects. It never writes `readingOrder`, since it always reads a document's tossups and then its
  bonuses. It writes `gameFormat` from `-g`/`--gameFormat` on the command line (`acf`, `macf`, `pace`, or a
  JSON file), `?gameFormat=` on the API (the named formats only), or `GameFormat` on `JsonPacketCompilerOptions`,
  and logs a warning when a power marker isn't in any tossup. It writes plain YAPP when a packet has no anchors
  and no game format.
- **QEMS** (producer): `qems2/qsub/yapp_export.py` writes it; `qems2/qsub/packet_set_importer.py`
  reads it back, mapping `<pg>` to QEMS's own `\P` markers. Exported from a set's page
  via "Export Packetized YAPP2 JSON", or from the export options form with
  "Interlace tossups and bonuses" ticked to get a `readingOrder`.
- **MODAQ** (consumer): `src/parser/FormattedTextParser.ts` parses `<pg>` into
  `IFormattedText.pronunciationTarget`; `src/components/PacketLoaderController.ts`
  prefers the `anchored` fields and validates `readingOrder` (keeping it only when it
  covers every question exactly once); `src/state/CustomExport.ts` writes both back out.
  MODAQ already plays a packet interlaced — a tossup, then its bonus — so `readingOrder`
  does not change how a game runs there; it is kept so a load/export round trip doesn't
  silently drop it. That repo and QEMS carry copies of this document.
- `gameFormat` (1.2) is new: QEMS doesn't write it and MODAQ doesn't apply it yet. A
  reader that follows the version rule above still reads a 1.2 file, ignoring
  `gameFormat`.

## Why not other approaches

- **A new tag directly in `question`** — breaks rule 1: existing readers show a literal
  `<pg>`.
- **Character offsets in a parallel array** — keeps the text pristine, but offsets are
  ambiguous (relative to the raw HTML, or the rendered text?) and silently rot when
  anything edits the string. Anchors that travel inside the text can't desynchronize
  from it.
- **A separate sidecar file** — packets are passed around as single files; anything not
  in the JSON gets lost. This is also why `gameFormat` lives in each packet rather than
  in a set-level file.
- **Power positions as word indexes** — the marker is already in the question text, so
  naming it ties the power to the text; an index would go stale when the question is
  edited, and would disagree with readers that count words differently.
