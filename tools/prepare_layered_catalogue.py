"""Register already generated image_gen plates. Does not generate/edit raster images."""
import copy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'SummonersTable/Assets'
CONFIG = ASSETS / 'StreamingAssets/Config'
LAYERS = ASSETS / 'LayeredCards/Resources/LayeredCards/Layers'


def write(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def main():
    catalogue = json.loads((CONFIG / 'cards.json').read_text(encoding='utf-8-sig'))['cards']
    data = json.loads((CONFIG / 'layered-cards.json').read_text(encoding='utf-8-sig'))
    existing = {c['cardName']: c for c in data['cards']}
    framing = {f['cardName']: f for f in data['framing']}
    result = []
    for card in catalogue:
        card_id, name = card['id'], card['name']
        if card_id in ('C02', 'C08', 'S01'):
            art = existing[name]
            if card_id == 'C08':
                art['subject']['singleSource'] = True
            result.append(art)
            continue
        for part in ('background', 'subject'):
            assert (LAYERS / card_id / (part + '.png')).is_file(), f'Missing {card_id}/{part}'
        frame = framing[name]
        zoom = frame['zoom']
        # Preserve the previously reviewed landmark of the square original.
        # The base card's maximum illustration window is 590 by 374.
        aspect = 590 / 374
        subject = dict(texture=f'LayeredCards/Layers/{card_id}/subject', fit='cover',
                       offsetX=round((.5-frame['focusX'])*zoom, 5),
                       offsetY=round((.5-frame['focusY'])*aspect*zoom, 5),
                       zoom=zoom, depth=0, foil=0)
        if card_id == 'S07':
            # Both onlookers and the dice are essential to this vertical composition.
            subject.update(fit='contain', offsetX=0, offsetY=0, zoom=1.02)
        result.append(dict(cardName=name, responsePower=1.5,
            background=dict(texture=f'LayeredCards/Layers/{card_id}/background', fit='cover',
                            offsetX=0, offsetY=0, zoom=1.3, depth=.085, foil=0),
            subject=dict(singleSource=True, offsetX=0, offsetY=0, zoom=1,
                         depth=.006, foil=.24, rear=subject, foreground=copy.deepcopy(subject))))
    assert len(result) == len(catalogue) == 30
    data['cards'] = result
    write(CONFIG / 'layered-cards.json', data)
    write(ASSETS / 'Resources/ConfigDefaults/layered-cards.json', data)
    main_file = CONFIG / 'main.json'
    main_data = json.loads(main_file.read_text(encoding='utf-8-sig'))
    main_data['managers']['layered-cards'] = data
    write(main_file, main_data)
    print('Registered 30 cards; reference compositions retained; working/default/main JSON synchronized.')


if __name__ == '__main__':
    main()
