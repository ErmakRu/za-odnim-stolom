using System;
using UnityEngine;
using UnityEngine.UI;
namespace SummonersTable
{
    public sealed class MatchOptionsView:MonoBehaviour
    {
        public Button wizards,commanders;
        public Toggle power,depth;
        public Text explanation;
        public Color selected=new Color(.94f,.66f,.25f),unselected=new Color(.22f,.14f,.09f);
        public Action<MatchOptions> changed;
        MatchOptions value=new MatchOptions();bool editable;
        void Awake()
        {
            wizards.onClick.AddListener(()=>Change(o=>o.mode=MatchOptions.Wizards));commanders.onClick.AddListener(()=>Change(o=>o.mode=MatchOptions.Commanders));
            power.onValueChanged.AddListener(v=>Change(o=>o.limitPower=v));depth.onValueChanged.AddListener(v=>Change(o=>o.cards3D=v));
        }
        void Change(Action<MatchOptions> update){if(!editable)return;var next=value.Copy();update(next);changed?.Invoke(next);}
        public void Present(MatchOptions options,bool canEdit)
        {
            selected=UserSettings.Data.highlightColor;if(ConfigRuntime.Available){var material=ConfigRuntime.Assets.Get<Material>(ConfigRuntime.Current.ui.highlightMaterial);wizards.targetGraphic.material=commanders.targetGraphic.material=material;}
            value=options;editable=canEdit;wizards.interactable=commanders.interactable=power.interactable=depth.interactable=canEdit;
            wizards.targetGraphic.color=options.IsCommanders?unselected:selected;commanders.targetGraphic.color=options.IsCommanders?selected:unselected;
            power.SetIsOnWithoutNotify(options.limitPower);depth.SetIsOnWithoutNotify(options.cards3D);
            var rules=ConfigRuntime.ActiveCatalog?.rules??new RulesDef();
            explanation.text=(options.IsCommanders?rules.commandersQte+" QTE на ход · стоимость напечатана на карте":rules.wizardSpells+" заклинаний ИЛИ "+rules.wizardMixedSpells+" заклинаний + "+rules.wizardCreatures+" существ")+"\n"+(options.limitPower?"Усиления ограничены":"Усиления складываются без предела")+(canEdit?" · выбирает лидер":" · правила лидера");
        }
    }
}
