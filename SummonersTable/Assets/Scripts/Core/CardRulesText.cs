namespace SummonersTable
{
    public static class CardRulesText
    {
        public static void Split(CardDef card,MatchOptions options,out string main,out string limits)
        {
            var primary=new System.Collections.Generic.List<string>();var notes=new System.Collections.Generic.List<string>();
            foreach(var part in System.Text.RegularExpressions.Regex.Split(For(card,options),@"(?<=[.!?;])\s+"))
            {
                // A trailing cap must not turn the actual action into small print.
                var inline=System.Text.RegularExpressions.Regex.Match(part,@"^(.+?),\s*((?:не выше|не более|максимум)\b.+)$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if(inline.Success)
                {
                    primary.Add(inline.Groups[1].Value.TrimEnd()+".");var cap=inline.Groups[2].Value.Trim();
                    notes.Add(char.ToUpperInvariant(cap[0])+cap.Substring(1));continue;
                }
                bool restriction=System.Text.RegularExpressions.Regex.IsMatch(part,@"(?i)не более|не выше|максимум|ограничен|не складывается|^усталость не|^ответный урон не|^этот урон не|^слот должен|^при срыве|^копия не атакует");
                (restriction?notes:primary).Add(part.Trim());
            }
            main=string.Join(" ",primary);limits=string.Join("\n",notes).Replace(";\n","; ");
        }
        static readonly string[] caps={
            " Несколько таких эффектов дают не более 2 карт.",
            "Общая защита существ не выше 2; ","Общая защита существ не выше 3; ",
            " Сумма таких усилений не выше +2.",
            " Все подобные помехи вместе добавляют не более 2 символов.",
            "Максимум 2; ",
            " Дополнительный добор от существ ограничен 2 картами.",
            " Не более 2 HP за одну атаку от всех таких эффектов.",
            " Общая прибавка не выше +2.",
            " Несколько принтеров дают не более 2 карт.",
            " Общий штраф от таких существ не выше 2.",
            " Все такие существа вместе лечат не более 4 HP за ход.",
            "Общий штраф не выше 4 секунд; ",
            " Несколько таких эффектов дают не более +4 секунд."
        };
        public static string For(CardDef card,MatchOptions options)
        {
            string text=card.rules;
            if(options==null||options.limitPower)return text;
            foreach(var cap in caps)text=text.Replace(cap,"");
            if(card.effect=="forgive")return "Каждое такое существо прощает ещё один промах в каждом вашем QTE: он не считается ошибкой и не отнимает время.";
            if(card.effect=="riskBoost")text=text.Replace("и не складывается","и складывается с повторными розыгрышами");
            return text.Replace(". усталость",". Усталость").Replace(". ответный",". Ответный").Replace(". итоговый",". Итоговый");
        }
    }
}
