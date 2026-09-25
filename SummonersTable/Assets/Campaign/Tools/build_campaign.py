"""One-time conversion of the branch's story.json into battle-grouped comic data.
Run manually; this does not overwrite author edits during import/Play.
"""
import json,re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
ASSETS=ROOT/'SummonersTable/Assets'
source=json.loads((ASSETS/'StreamingAssets/Config/story.json').read_text(encoding='utf-8-sig'))
decks={'0-1':'noise','0-2':'cozy','0-3':'tricks','boss_golden':'tricks'}
actual_decks=json.loads((ASSETS/'StreamingAssets/Config/decks.json').read_text(encoding='utf-8-sig'))['decks']
decks['0-2']=actual_decks[1]['id']
portraits={'Бальтазар':'C09','ЖабаДушэс':'C05','Малфас':'C06','Валафара':'C04','Голиаф':'C10','Белиал':'C09','Бахрам':'C01','Гримвальд':'C18','Абаддон':'C15','Мальгис':'C07','Мортиция':'C16','Аспид':'C13','Вульгор':'C11','Геката':'C17','Астарот':'C08','Бастет':'C02','Кронос':'C12','Азазель':'S07','Собачка':'C03'}
sounds={p.name for p in (ASSETS/'ThirdParty/Audio/Card_Game').rglob('*.wav')}
unresolved=set()
def sound(s):
    if not s or s=='Mute' or s.startswith('action:'):return s
    s=s.split('<br>')[0].replace('`','')
    if s in sounds:return s
    stem=s.removesuffix('.wav').removeprefix('Card_Game_')
    for candidate in [f'Card_Game_{stem}.wav',f'Card_Game_{stem.replace("Movement_","Movement_")}.wav']:
        if candidate in sounds:return candidate
    matches=[x for x in sounds if x.removesuffix('.wav').endswith(stem)]
    if matches:return sorted(matches)[0]
    unresolved.add(s);return ''

# Only instructions contradicted by today's engine are adapted. Narrative dialogue stays intact.
adaptations={
'1.4':'Ничего ты не понимаешь в боевой эстетике! Это элитный Медведь-обниматель! Он защищает твоего героя. В начале каждого твоего хода берёшь из колоды одну карту. Больше карт в руке — шире стратегический манёвр. Тяни!',
'1.9':'Во время своего хода сам выбери существо на столе и потяни от него стрелку к цели. Стрелка в центр означает случайного противника. Если цель не назначена, при завершении хода существо тоже атакует случайного врага.',
'1.10':'Нажимаешь «Закончить ход» — и твои существа атакуют по очереди. Цель — выбранный враг или его существо; без назначения — случайный вражеский герой. Реакции разрешены во время розыгрыша карты, но не во время этих атак.'
}
def actor(char):
    if not char:return dict(art='',label='',portrait=False,scale=1,offsetX=0,offsetY=0)
    clean=char.replace('_в_клетке','').replace('Владыка_','')
    if clean=='Малхорат':art='Art/Others/mainKing';portrait=False
    else:art='Art/'+portraits.get(clean,'C09');portrait=True
    return dict(art=art,label=clean,portrait=portrait,scale=1,offsetX=0,offsetY=0)

def frame_lines(steps,title,prefix):
    result=[]
    for step in steps:
        sprites=step.get('sprites',[])
        others=[s for s in sprites if not s['character'].startswith('Йорик')]
        opponent=next((s['character'] for s in others if s['position']=='R'),others[-1]['character'] if others else '')
        bg=step.get('background','')
        art='LayeredCards/Layers/S01/background' if 'tavern' in bg or 'barn' in bg else 'LayeredCards/Layers/C02/background'
        key=(bg,opponent)
        if not result or result[-1]['_key']!=key:
            result.append(dict(id=f'{prefix}-f{len(result)+1:02}',title=title,background=art,
                left=dict(art='Art/Jester/jester_basic' if sprites else '',label='Йорик' if sprites else '',portrait=False,scale=1,offsetX=0,offsetY=0),right=actor(opponent),lines=[],_key=key))
        happy=any(s['character'].startswith('Йорик') and any(x in s.get('emotion','') for x in ['радость','победа','улыбка','смех','гордость']) for s in sprites)
        result[-1]['lines'].append(dict(sourceId=step['id'],speaker=step['speaker'],text=adaptations.get(step['id'],step['text']),leftArt='Art/Jester/'+('jester_happy' if happy else 'jester_basic'),sfx=sound(step.get('audio',{}).get('sfx','')),ambience=sound(step.get('audio',{}).get('bgm',''))))
    for f in result:f.pop('_key')
    return result

intro=[]
for scene in source['scenes'][:3]:intro+=frame_lines(scene['steps'],scene['title'],scene['sceneId'])
chapters=[];pending=[]
for scene in source['scenes'][3:-1]:
    steps=scene['steps'];start=0
    triggers=[i for i,s in enumerate(steps) if s.get('action',{}).get('actionType')=='START_BATTLE']
    for ti,end in enumerate(triggers):
        trigger=steps[end];a=trigger['action'];enemy=next(s['character'] for s in reversed(trigger['sprites']) if s['position']=='R')
        after_end=(triggers[ti+1] if ti+1<len(triggers) else len(steps))
        after_steps=steps[end+1:after_end]
        # Multi-battle scenes alternate one challenge with one result line.
        before_steps=pending+steps[start:end+1];pending=[]
        n=len(chapters)+1
        chapters.append(dict(id=a['target'],title=actor(enemy)['label'],opponentName=actor(enemy)['label'],opponentDeck=decks[a['parameter']],opponentHero=['badger','lion','deer','dog','owl','lizard','rabbit','rat'][(n-1)%8],mode='commanders' if n in [4,8,12,16] else 'wizards',seed=1700+n,
            before=frame_lines(before_steps,scene['title'],a['target']+'-before'),after=frame_lines(after_steps,scene['title'],a['target']+'-after')))
        start=after_end
ending=frame_lines(source['scenes'][-1]['steps'],source['scenes'][-1]['title'],'ending')
book=dict(schemaVersion=1,title='Пир Хохота',defaultDeck='noise',charactersPerSecond=65,ambienceVolume=.22,sfxVolume=.6,introduction=intro,chapters=chapters,ending=ending)
dest=ASSETS/'Campaign/Resources/Campaign/campaign.json';dest.parent.mkdir(parents=True,exist_ok=True)
dest.write_text(json.dumps(book,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
lines=[l for fs in [intro,ending]+[fs for c in chapters for fs in [c['before'],c['after']]] for f in fs for l in f['lines']]
original=[s['id'] for scene in source['scenes'] for s in scene['steps']]
assert len(chapters)==17
assert sorted(l['sourceId'] for l in lines)==sorted(original),'Every source line must occur exactly once'
report=dict(chapters=len(chapters),lines=len(lines),source='Assets/StreamingAssets/Config/story.json',sourceBranch='script&chars',adaptedTutorial=list(adaptations),placeholderPortraits=portraits,unresolvedAudio=sorted(unresolved))
(ASSETS/'Campaign/coverage.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
