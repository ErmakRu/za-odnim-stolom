using System;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class CampaignComicView:MonoBehaviour
    {
        public GameObject comic,hub;
        public RawImage background,left,right;
        public Image rightBorder;
        public Text title,speaker,body,progress,leftName,rightName,nextLabel,hubTitle,hubStatus,deckLabel,guide;
        public Button next,previous,skip,menu,resume,newGame,deckPrevious,deckNext,hubMenu;
        public AudioSource ambience,sfx;
        public CampaignMedia media;
        public Action nextAction,previousAction,skipAction,menuAction,resumeAction,newAction,deckPrevAction,deckNextAction;
        public string FullText{get;private set;}="";
        public bool Typing=>shown<FullText.Length;
        float shown;CampaignBook book;
        public void Bind(CampaignBook config)
        {
            book=config;
            next.onClick.RemoveAllListeners();next.onClick.AddListener(Advance);
            previous.onClick.RemoveAllListeners();previous.onClick.AddListener(()=>previousAction?.Invoke());
            skip.onClick.RemoveAllListeners();skip.onClick.AddListener(()=>skipAction?.Invoke());
            menu.onClick.RemoveAllListeners();menu.onClick.AddListener(()=>menuAction?.Invoke());
            hubMenu.onClick.RemoveAllListeners();hubMenu.onClick.AddListener(()=>menuAction?.Invoke());
            resume.onClick.RemoveAllListeners();resume.onClick.AddListener(()=>resumeAction?.Invoke());
            newGame.onClick.RemoveAllListeners();newGame.onClick.AddListener(()=>newAction?.Invoke());
            deckPrevious.onClick.RemoveAllListeners();deckPrevious.onClick.AddListener(()=>deckPrevAction?.Invoke());
            deckNext.onClick.RemoveAllListeners();deckNext.onClick.AddListener(()=>deckNextAction?.Invoke());
        }
        public void Advance(){if(Typing){shown=FullText.Length;body.text=FullText;}else nextAction?.Invoke();}
        public void Present(ComicFrame frame,ComicLine line,string heading,string counter,bool canGoBack,bool last,string finalLabel)
        {
            gameObject.SetActive(true);hub.SetActive(false);comic.SetActive(true);
            background.texture=Resources.Load<Texture2D>(frame.background);
            title.text=heading;progress.text=counter;speaker.text=line.speaker;FullText=line.text;shown=book.charactersPerSecond<=0?FullText.Length:0;body.text=shown>0?FullText:"";
            SetActor(left,frame.left,string.IsNullOrEmpty(line.leftArt)?frame.left.art:line.leftArt,new Vector2(-460,104));
            SetActor(right,frame.right,frame.right.art,new Vector2(460,104));
            rightBorder.gameObject.SetActive(frame.right.portrait&&!string.IsNullOrEmpty(frame.right.art));
            rightBorder.rectTransform.anchoredPosition=right.rectTransform.anchoredPosition;
            rightBorder.rectTransform.sizeDelta=right.rectTransform.sizeDelta+Vector2.one*12;
            leftName.text=frame.left.label;rightName.text=frame.right.label;
            previous.interactable=canGoBack;nextLabel.text=last?finalLabel:"Дальше  →";
            if(line.ambience=="Mute")ambience.Stop();
            else if(!string.IsNullOrEmpty(line.ambience)){var clip=media.Find(line.ambience);if(clip!=null&&ambience.clip!=clip){ambience.clip=clip;ambience.Play();}}
            if(!string.IsNullOrEmpty(line.sfx)){if(line.sfx.StartsWith("action:"))ConfigAudio.Play(line.sfx.Substring(7));else{var clip=media.Find(line.sfx);if(clip!=null)sfx.PlayOneShot(clip);}}
        }
        void SetActor(RawImage image,ComicActor actor,string art,Vector2 origin)
        {
            image.gameObject.SetActive(!string.IsNullOrEmpty(art));if(string.IsNullOrEmpty(art))return;
            image.texture=Resources.Load<Texture2D>(art);var rect=image.rectTransform;
            var texture=image.texture;float maxW=actor.portrait?470:650,maxH=actor.portrait?470:640;
            float fit=Mathf.Min(maxW/texture.width,maxH/texture.height)*actor.scale;
            rect.sizeDelta=new Vector2(texture.width*fit,texture.height*fit);rect.anchoredPosition=origin+new Vector2(actor.offsetX,actor.offsetY);
            image.color=Color.white;
        }
        public void ShowHub(CampaignBook config,CampaignProgress save,Catalog catalog)
        {
            gameObject.SetActive(true);comic.SetActive(false);hub.SetActive(true);ambience.Stop();
            hubTitle.text=config.title;hubStatus.text=save.phase=="done"?$"Все {config.chapters.Length} поединков пройдены":$"Поединок {save.chapter+1} / {config.chapters.Length} · {config.chapters[save.chapter].opponentName}";
            var deck=catalog.Deck(save.deck)??catalog.Deck(config.defaultDeck);deckLabel.text=deck.name;guide.text=deck.guide;
            resume.GetComponentInChildren<Text>().text=save.phase=="done"?"Посмотреть эпилог":"Продолжить путь";
        }
        public void Hide(){ambience.Stop();gameObject.SetActive(false);}
        void Update()
        {
            if(book==null)return;
            ambience.volume=book.ambienceVolume*UserSettings.Volume(AudioBus.Ambience);sfx.volume=book.sfxVolume*UserSettings.Volume(AudioBus.Effects);
            if(!comic.activeSelf)return;
            if(Typing){shown=Mathf.Min(FullText.Length,shown+Time.unscaledDeltaTime*book.charactersPerSecond);body.text=FullText.Substring(0,Mathf.FloorToInt(shown));}
            if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return))Advance();
            else if(Input.GetKeyDown(KeyCode.LeftArrow))previousAction?.Invoke();
        }
    }
}
