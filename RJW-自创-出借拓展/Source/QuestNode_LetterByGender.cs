using RimWorld;
using RimWorld.QuestGen;
using Verse;
using System.Collections.Generic;

public class QuestNode_LetterByGender : QuestNode
{
    public SlateRef<string> label;
    public SlateRef<string> textFemale;
    public SlateRef<string> textMale;
    public SlateRef<string> textDefault;
    public SlateRef<LetterDef> letterDef;
    public SlateRef<LookTargets> lookTargets;
    public SlateRef<Thing> shuttle;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Gender? gender = null;

        Thing shuttleThing = shuttle.GetValue(slate);
        if (shuttleThing != null)
        {
            // 修正1：用 'as' 进行安全转换，避免强制转换报错
            TransportShip transportShip = shuttleThing as TransportShip;
            if (transportShip != null)
            {
                // 修正2：尝试用 pawnList 获取殖民者（如果这个属性存在）
                List<Pawn> pawns = transportShip.pawnList; 
                if (pawns != null && pawns.Count > 0)
                {
                    gender = pawns[0].gender;
                }
            }
        }

        // 如果上面的方法没拿到，再尝试从 Slate 里读
        if (gender == null)
        {
            Gender g;
            if (slate.TryGet<Gender>("lentPawnGender", out g))
                gender = g;
        }

        string finalText;
        if (gender == Gender.Female)
            finalText = textFemale.GetValue(slate);
        else if (gender == Gender.Male)
            finalText = textMale.GetValue(slate);
        else
            finalText = textDefault.GetValue(slate);

        Find.LetterStack.ReceiveLetter(
            label.GetValue(slate),
            finalText,
            letterDef.GetValue(slate),
            lookTargets.GetValue(slate)
        );
    }

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }
}