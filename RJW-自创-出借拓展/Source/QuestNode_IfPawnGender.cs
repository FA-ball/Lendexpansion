using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace BrothelExpansion
{
    public class QuestNode_IfPawnGender : QuestNode
    {
        [NoTranslate]
        public SlateRef<Pawn> pawn;

        public QuestNode nodeMale;
        public QuestNode nodeFemale;

        protected override void RunInt()
        {
            Pawn p = pawn.GetValue(slate);
            if (p == null) 
            {
                // 安全处理：没有pawn时走女性分支（或你想默认的）
                nodeFemale?.Run();
                return;
            }

            if (p.gender == Gender.Male)
                nodeMale?.Run();
            else
                nodeFemale?.Run();
        }

        protected override bool TestRunInt(Slate slate)
        {
            // 关键：TestRun 时直接通过，不验证pawn
            return true;
        }
    }
}