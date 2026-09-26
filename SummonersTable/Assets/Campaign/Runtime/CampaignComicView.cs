using System;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class CampaignComicView:MonoBehaviour
    {
        public GameObject comic,hub;
        public RawImage background,left,right,rewardIcon;
        public Image rightBorder;
        public TavernPanel dialoguePanel,namePanel;
        public GameObject rewardRoot;
        public Text rewardLabel;
        public Text title,speaker,body,progress,leftName,rightName,nextLabel,hubTitle,hubStatus,deckLabel,guide;
        public Button next,previous,skip,menu,resume,newGame,deckPrevious,deckNext,hubMenu;
        public AudioSource ambience,sfx;
        public CampaignMedia media;
        public Action nextAction,previousAction,skipAction,menuAction,resumeAction,newAction,deckPrevAction,deckNextAction;
        public Action<int> segmentChanged;
        public string FullText{get;private set;}="";
        public bool Typing=>shown<FullText.Length;
        public int Segment{get;private set;}
        public int SegmentCount=>pages.Length;
        public bool IsThought{get;private set;}
        float shown;double pendingClick=-1;CampaignBook book;string[] pages={""};string counterText,finalText;bool lastLine,canBack;
        public void Bind(CampaignBook config)
        {
            book=config;
            next.onClick.RemoveAllListeners();next.onClick.AddListener(Advance);
            previous.onClick.RemoveAllListeners();previous.onClick.AddListener(Previous);
            skip.onClick.RemoveAllListeners();skip.onClick.AddListener(()=>skipAction?.Invoke());
            menu.onClick.RemoveAllListeners();menu.onClick.AddListener(()=>menuAction?.Invoke());
            hubMenu.onClick.RemoveAllListeners();hubMenu.onClick.AddListener(()=>menuAction?.Invoke());
            resume.onClick.RemoveAllListeners();resume.onClick.AddListener(()=>resumeAction?.Invoke());
            newGame.onClick.RemoveAllListeners();newGame.onClick.AddListener(()=>newAction?.Invoke());
            deckPrevious.onClick.RemoveAllListeners();deckPrevious.onClick.AddListener(()=>deckPrevAction?.Invoke());
            deckNext.onClick.RemoveAllListeners();deckNext.onClick.AddListener(()=>deckNextAction?.Invoke());
        }
        public void Reveal(){shown=FullText.Length;body.text=FullText;}
        public void Advance(){pendingClick=-1;if(Typing)Reveal();else AdvancePage();}
        public void AdvancePage(){pendingClick=-1;if(Segment+1<pages.Length)UseSegment(Segment+1);else nextAction?.Invoke();}
        public void Previous(){pendingClick=-1;if(Segment>0)UseSegment(Segment-1);else previousAction?.Invoke();}
        public void PointerClick(int clickCount)
        {
            // Defer a single click so the first click of a double click cannot skip
            // one extra line when the current text has already finished appearing.
            if(clickCount%2==0){pendingClick=-1;AdvancePage();}
            else pendingClick=Time.unscaledTimeAsDouble+.30;
        }
        public void UseSegment(int index)
        {
            Segment=Mathf.Clamp(index,0,pages.Length-1);FullText=pages[Segment];
            if(IsThought)FullText="«"+FullText.Trim('«','»','“','”','"')+"»";
            body.fontStyle=IsThought?FontStyle.Italic:FontStyle.Normal;
            shown=0;body.text="";pendingClick=-1;
            progress.text=counterText+$" · Фрагмент {Segment+1}/{pages.Length}";
            nextLabel.text=lastLine&&Segment==pages.Length-1?finalText:"Дальше  →";
            previous.interactable=canBack||Segment>0;segmentChanged?.Invoke(Segment);
        }
        public void Present(ComicFrame frame,ComicLine line,string heading,string counter,bool canGoBack,bool last,string finalLabel)
        {
            gameObject.SetActive(true);hub.SetActive(false);comic.SetActive(true);
            background.texture=Resources.Load<Texture2D>(frame.background);title.text=heading;
            bool narrator=book.IsNarrator(line);var character=book.Character(line);
            bool hero=!narrator&&(character?.type=="main"||character==null&&line.speaker.StartsWith("ШУТ",StringComparison.OrdinalIgnoreCase));
            string kind=narrator?"narration":character?.type??"npc";var palette=book.Palette(kind);
            ColorUtility.TryParseHtmlString(palette.body,out var fill);ColorUtility.TryParseHtmlString(palette.border,out var edge);ColorUtility.TryParseHtmlString(palette.name,out var name);
            if(dialoguePanel!=null){dialoguePanel.color=fill;dialoguePanel.border=edge;dialoguePanel.SetVerticesDirty();}
            if(namePanel!=null){namePanel.gameObject.SetActive(!narrator);namePanel.color=name;namePanel.border=edge;namePanel.SetVerticesDirty();}
            speaker.gameObject.SetActive(!narrator);speaker.text=character?.name??line.speaker;
            left.gameObject.SetActive(false);right.gameObject.SetActive(false);rightBorder.gameObject.SetActive(false);
            leftName.gameObject.SetActive(false);rightName.gameObject.SetActive(false);
            if(!narrator)
            {
                var actor=character?.Actor()??(hero?frame.left:frame.right);
                bool jester=character==null||character.id=="jester"||character.id=="jester-child";
                string art=!string.IsNullOrEmpty(line.artOverride)?line.artOverride:hero&&jester&&!string.IsNullOrEmpty(line.leftArt)?line.leftArt:actor.art;
                SetActor(hero?left:right,actor,art,new Vector2(hero?-400:400,-345));
            }
            rewardRoot?.SetActive(!string.IsNullOrEmpty(line.rewardIcon));
            if(rewardIcon!=null){rewardIcon.texture=string.IsNullOrEmpty(line.rewardIcon)?null:Resources.Load<Texture2D>(line.rewardIcon);rewardLabel.text=line.rewardLabel;if(rewardIcon.texture!=null){float fit=Mathf.Min(168f/rewardIcon.texture.width,204f/rewardIcon.texture.height);var rect=rewardIcon.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,22);rect.sizeDelta=new Vector2(rewardIcon.texture.width*fit,rewardIcon.texture.height*fit);}}
            IsThought=line.kind=="thought";pages=ComicText.Split(line.text,book.textPageLength);counterText=counter;finalText=finalLabel;lastLine=last;canBack=canGoBack;
            UseSegment(0);
            if(line.ambience=="Mute")ambience.Stop();
            else if(!string.IsNullOrEmpty(line.ambience)){var clip=media.Find(line.ambience);if(clip!=null&&ambience.clip!=clip){ambience.clip=clip;ambience.Play();}}
            if(!string.IsNullOrEmpty(line.sfx)){if(line.sfx.StartsWith("action:"))ConfigAudio.Play(line.sfx.Substring(7));else{var clip=media.Find(line.sfx);if(clip!=null)sfx.PlayOneShot(clip);}}
        }
        void SetActor(RawImage image,ComicActor actor,string art,Vector2 origin)
        {
            var texture=Resources.Load<Texture2D>(art);image.gameObject.SetActive(texture!=null);if(texture==null)return;
            image.texture=texture;var rect=image.rectTransform;
            float fit=Mathf.Min((actor.portrait?680:760)/(float)texture.width,900f/texture.height)*actor.scale;
            rect.sizeDelta=new Vector2(texture.width*fit,texture.height*fit);rect.anchoredPosition=origin+new Vector2(actor.offsetX,actor.offsetY);image.color=Color.white;
        }
        public void ShowHub(CampaignBook config,CampaignProgress save,Catalog catalog)
        {
            pendingClick=-1;gameObject.SetActive(true);comic.SetActive(false);hub.SetActive(true);ambience.Stop();
            hubTitle.text=config.title;hubStatus.text=save.phase=="done"?$"Все {config.chapters.Length} поединков пройдены":$"Поединок {save.chapter+1} / {config.chapters.Length} · {config.chapters[save.chapter].opponentName}";
            var deck=catalog.Deck(save.deck)??catalog.Deck(config.defaultDeck);deckLabel.text=deck.name;guide.text=deck.guide;
            resume.GetComponentInChildren<Text>().text=save.phase=="done"?"Посмотреть эпилог":"Продолжить путь";
        }
        public void Hide(){pendingClick=-1;ambience.Stop();gameObject.SetActive(false);}
        void Update()
        {
            if(book==null)return;
            ambience.volume=book.ambienceVolume*UserSettings.Volume(AudioBus.Ambience);sfx.volume=book.sfxVolume*UserSettings.Volume(AudioBus.Effects);
            if(!comic.activeSelf)return;
            if(Typing){shown=Mathf.Min(FullText.Length,shown+Time.unscaledDeltaTime*Mathf.Max(1,book.charactersPerSecond));body.text=FullText.Substring(0,Mathf.FloorToInt(shown));}
            if(pendingClick>=0&&Time.unscaledTimeAsDouble>=pendingClick)Advance();
            if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return))Advance();
            else if(Input.GetKeyDown(KeyCode.LeftArrow))Previous();
        }
    }
}
