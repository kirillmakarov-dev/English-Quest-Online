#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class DialogueGenerator : MonoBehaviour
{
    [MenuItem("Tools/English Kingdom/Content/Generate Game Dialogues")]
    public static void GenerateGameDialogues()
    {
        // GenerateBenjiDialogue();
        // GenerateProfessorDialogue();
        // GenerateShadowGuardDialogue();
        // GenerateMountainClimberDialogue();
        // GenerateMuseumGuardDialogue();
        // GenerateOldGuardDemoDialogue();
        // GenerateOldGuardFullDialogue();
        // GenerateRidingGuideDialogue();
        GenerateLostCoinNPCDialogue();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AppLog.Info("All dialogues generated successfully!");
    }

    private static void GenerateBenjiDialogue()
    {
        string directoryPath = "Assets/_OurAssets/ScriptableObjects/Dialogue/Benji";
        EnsureDirectoryExists(directoryPath);

        // Create Instances
        DialogueNode node1 = ScriptableObject.CreateInstance<DialogueNode>(); node1.name = "Benji_01_Intro";
        DialogueNode node2 = ScriptableObject.CreateInstance<DialogueNode>(); node2.name = "Benji_02_Plot";
        DialogueNode node3 = ScriptableObject.CreateInstance<DialogueNode>(); node3.name = "Benji_03_Mission";
        DialogueNode node4 = ScriptableObject.CreateInstance<DialogueNode>(); node4.name = "Benji_04_HighPlaces";

        // Create Assets
        CreateAssetIfNeeded(node1, directoryPath);
        CreateAssetIfNeeded(node2, directoryPath);
        CreateAssetIfNeeded(node3, directoryPath);
        CreateAssetIfNeeded(node4, directoryPath);

        // Configure Nodes
        // Node 1
        ConfigureNode(node1, "בנז'י הבונה",
            "היי! אתם בטח האבירים ששומר השער דיבר עליהם. מזל שהגעתם, אני בבעיה רצינית!",
            new DialogueChoice { choiceText = "המשך", nextNode = node2 });

        // Node 2
        ConfigureNode(node2, "בנז'י הבונה",
            "הרוח של הנבל פיזרה את האותיות הקסומות בכל העמק. בלי האותיות האלו, השער לעיר המרכזית פשוט לא יפתח.",
            new DialogueChoice { choiceText = "המשך", nextNode = node3 });

        // Node 3
        ConfigureNode(node3, "בנז'י הבונה",
            "חפשו את האותיות A, B, ו-C שמסתתרות בין הפלטפורמות. אתם חייבים להביא אותן לכאן ולהניח אותן במקום שלהן על השער.",
            new DialogueChoice { choiceText = "המשך", nextNode = node4 });

        // Node 4
        ConfigureNode(node4, "בנז'י הבונה",
            "שימו לב – חלק מהאותיות נמצאות במקומות גבוהים! תצטרכו לקפוץ על הפלטפורמות הנעות ולעזור אחד לשני. אני מחכה לכם כאן!",
            new DialogueChoice { choiceText = "סיום", nextNode = null });
        node4.actionType = DialogueActionType.CloseDialogue;

        SetDirty(node1, node2, node3, node4);
    }

    private static void GenerateProfessorDialogue()
    {
        string directoryPath = "Assets/_OurAssets/ScriptableObjects/Dialogue/Professor";
        EnsureDirectoryExists(directoryPath);

        // Create Instances
        DialogueNode node1 = ScriptableObject.CreateInstance<DialogueNode>(); node1.name = "Professor_01_Intro";
        DialogueNode node2 = ScriptableObject.CreateInstance<DialogueNode>(); node2.name = "Professor_02_Explanation";
        DialogueNode node3 = ScriptableObject.CreateInstance<DialogueNode>(); node3.name = "Professor_03_Encouragement";

        // Create Assets
        CreateAssetIfNeeded(node1, directoryPath);
        CreateAssetIfNeeded(node2, directoryPath);
        CreateAssetIfNeeded(node3, directoryPath);

        // Configure Nodes
        // Node 1
        ConfigureNode(node1, "הפרופסור המפוזר",
            "הו! בדיוק בזמן! אני מנסה לפתוח את התיבות האלו כבר שנים, אבל הקסם שלהן דורש כתיבה מדויקת באנגלית!",
            new DialogueChoice { choiceText = "המשך", nextNode = node2 });

        // Node 2
        ConfigureNode(node2, "הפרופסור המפוזר",
            "יש כאן 4 תיבות. על כל תיבה כתובה המילה בעברית, אבל כדי שהיא תפתח – אתם חייבים להקליד את המילה באותיות אנגליות.",
            new DialogueChoice { choiceText = "המשך", nextNode = node3 });

        // Node 3
        ConfigureNode(node3, "הפרופסור המפוזר",
            "בכל פעם שתפתחו תיבה, ישתחרר ניצוץ של אנרגיה לשער הגדול. פתחו את כל הארבע, והדרך לעולם הבא תהיה פתוחה לפניכם. בהצלחה, חוקרים צעירים!",
            new DialogueChoice { choiceText = "סיום", nextNode = null });
        node3.actionType = DialogueActionType.CloseDialogue;

        SetDirty(node1, node2, node3);
    }

    private static void GenerateShadowGuardDialogue()
    {
        string directoryPath = "Assets/_OurAssets/ScriptableObjects/Dialogue/ShadowGuard";
        EnsureDirectoryExists(directoryPath);

        // Create Instances
        DialogueNode node1 = ScriptableObject.CreateInstance<DialogueNode>(); node1.name = "ShadowGuard_01_Warning";
        DialogueNode node2 = ScriptableObject.CreateInstance<DialogueNode>(); node2.name = "ShadowGuard_02_Mechanic";
        DialogueNode node3 = ScriptableObject.CreateInstance<DialogueNode>(); node3.name = "ShadowGuard_03_Instruction";

        // Create Assets
        CreateAssetIfNeeded(node1, directoryPath);
        CreateAssetIfNeeded(node2, directoryPath);
        CreateAssetIfNeeded(node3, directoryPath);

        // Configure Nodes
        // Node 1
        ConfigureNode(node1, "השומר הזקן",
            "עצרו! אנחנו בפתחו של האזור האפל. הנבל הציב כאן מפלצות צללים שחוסמות את המעבר לעולם הבא!",
            new DialogueChoice { choiceText = "המשך", nextNode = node2 });

        // Node 2
        ConfigureNode(node2, "השומר הזקן",
            "הקשיבו היטב... המפלצת שואגת צליל של אות. היא לא יכולה לסבול את האות הנכונה! אם תניחו את האות שהיא צועקת בתוך 'סלע האור' שכאן, הכוח שלה ישמיד אותה ויבנה לכם גשר.",
            new DialogueChoice { choiceText = "המשך", nextNode = node3 });

        // Node 3
        ConfigureNode(node3, "השומר הזקן",
            "ישנן ארבע מפלצות כאלו בדרך. אתם חייבים להקשיב לצליל (The Sound), למצוא את האות המתאימה, ולפנות את הדרך. היזהרו מהבורות, אבירים!",
            new DialogueChoice { choiceText = "סיום", nextNode = null });
        node3.actionType = DialogueActionType.CloseDialogue;

        SetDirty(node1, node2, node3);
    }

    private static void GenerateMountainClimberDialogue()
    {
        string directoryPath = "Assets/_OurAssets/ScriptableObjects/Dialogue/MountainClimber";
        EnsureDirectoryExists(directoryPath);

        DialogueNode node1 = ScriptableObject.CreateInstance<DialogueNode>(); node1.name = "MountainClimber_01_Stop";
        DialogueNode node2 = ScriptableObject.CreateInstance<DialogueNode>(); node2.name = "MountainClimber_02_Path";
        DialogueNode node3 = ScriptableObject.CreateInstance<DialogueNode>(); node3.name = "MountainClimber_03_Jump";
        DialogueNode node4 = ScriptableObject.CreateInstance<DialogueNode>(); node4.name = "MountainClimber_04_Timing";
        DialogueNode node5 = ScriptableObject.CreateInstance<DialogueNode>(); node5.name = "MountainClimber_05_Peak";
        DialogueNode node6 = ScriptableObject.CreateInstance<DialogueNode>(); node6.name = "MountainClimber_06_Go";

        CreateAssetIfNeeded(node1, directoryPath);
        CreateAssetIfNeeded(node2, directoryPath);
        CreateAssetIfNeeded(node3, directoryPath);
        CreateAssetIfNeeded(node4, directoryPath);
        CreateAssetIfNeeded(node5, directoryPath);
        CreateAssetIfNeeded(node6, directoryPath);

        ConfigureNode(node1, "מטפס ההרים",
            "עצרו רגע!",
            new DialogueChoice { choiceText = "המשך", nextNode = node2 });

        ConfigureNode(node2, "מטפס ההרים",
            "הדרך למעלה עוברת מסביב לסלע הענקי הזה.",
            new DialogueChoice { choiceText = "המשך", nextNode = node3 });

        ConfigureNode(node3, "מטפס ההרים",
            "תצטרכו לקפוץ מפלטפורמה לפלטפורמה ולהתקדם בסיבוב כלפי מעלה.",
            new DialogueChoice { choiceText = "המשך", nextNode = node4 });

        ConfigureNode(node4, "מטפס ההרים",
            "שימו לב טוב לאן אתם נוחתים... לפעמים צריך לחכות רגע ולקפוץ בזמן הנכון.",
            new DialogueChoice { choiceText = "המשך", nextNode = node5 });

        ConfigureNode(node5, "מטפס ההרים",
            "אם תמשיכו לטפס עד הפסגה, תגיעו למקום חשוב מאוד.",
            new DialogueChoice { choiceText = "המשך", nextNode = node6 });

        ConfigureNode(node6, "מטפס ההרים",
            "קדימה, בואו נראה אתכם מגיעים עד ללמעלה.",
            new DialogueChoice { choiceText = "סיום", nextNode = null });
        node6.actionType = DialogueActionType.CloseDialogue;

        SetDirty(node1, node2, node3, node4, node5, node6);
    }

    private static void GenerateMuseumGuardDialogue()
    {
        string directoryPath = "Assets/_OurAssets/ScriptableObjects/Dialogue/MuseumGuard";
        EnsureDirectoryExists(directoryPath);

        DialogueNode node1 = ScriptableObject.CreateInstance<DialogueNode>(); node1.name = "MuseumGuard_01_Welcome";
        DialogueNode node2 = ScriptableObject.CreateInstance<DialogueNode>(); node2.name = "MuseumGuard_02_Villain";
        DialogueNode node3 = ScriptableObject.CreateInstance<DialogueNode>(); node3.name = "MuseumGuard_03_NewChallenge";
        DialogueNode node4 = ScriptableObject.CreateInstance<DialogueNode>(); node4.name = "MuseumGuard_04_Collect";
        DialogueNode node5 = ScriptableObject.CreateInstance<DialogueNode>(); node5.name = "MuseumGuard_05_Restore";
        DialogueNode node6 = ScriptableObject.CreateInstance<DialogueNode>(); node6.name = "MuseumGuard_06_Flags";
        DialogueNode node7 = ScriptableObject.CreateInstance<DialogueNode>(); node7.name = "MuseumGuard_07_Go";

        CreateAssetIfNeeded(node1, directoryPath);
        CreateAssetIfNeeded(node2, directoryPath);
        CreateAssetIfNeeded(node3, directoryPath);
        CreateAssetIfNeeded(node4, directoryPath);
        CreateAssetIfNeeded(node5, directoryPath);
        CreateAssetIfNeeded(node6, directoryPath);
        CreateAssetIfNeeded(node7, directoryPath);

        ConfigureNode(node1, "שומר המוזיאון",
            "הגעתם למוזיאון האותיות.",
            new DialogueChoice { choiceText = "המשך", nextNode = node2 });

        ConfigureNode(node2, "שומר המוזיאון",
            "הנבל השתלט על המקום הזה וכלא את הכוח של האותיות באנגלית.",
            new DialogueChoice { choiceText = "המשך", nextNode = node3 });

        ConfigureNode(node3, "שומר המוזיאון",
            "בכל פעם שתיכנסו לכאן, יחכה לכם אתגר חדש.",
            new DialogueChoice { choiceText = "המשך", nextNode = node4 });

        ConfigureNode(node4, "שומר המוזיאון",
            "תצטרכו להקשיב, לזהות, ולאסוף את האותיות הנכונות.",
            new DialogueChoice { choiceText = "המשך", nextNode = node5 });

        ConfigureNode(node5, "שומר המוזיאון",
            "אם תצליחו, הכוח של המוזיאון יחזור, והסימנים של הנבל ייעלמו.",
            new DialogueChoice { choiceText = "המשך", nextNode = node6 });

        ConfigureNode(node6, "שומר המוזיאון",
            "כשהקסם יחזור, גם דגלי הממלכה יתנופפו כאן שוב.",
            new DialogueChoice { choiceText = "המשך", nextNode = node7 });

        ConfigureNode(node7, "שומר המוזיאון",
            "קדימה, בואו נראה מה מחכה לכם הפעם.",
            new DialogueChoice { choiceText = "סיום", nextNode = null });
        node7.actionType = DialogueActionType.CloseDialogue;

        SetDirty(node1, node2, node3, node4, node5, node6, node7);
    }

    private static void GenerateOldGuardDemoDialogue()
    {
        string directoryPath = "Assets/_OurAssets/ScriptableObjects/Dialogue/OldGuard_Demo";
        EnsureDirectoryExists(directoryPath);

        DialogueNode node1 = ScriptableObject.CreateInstance<DialogueNode>(); node1.name = "OldGuardDemo_01_Welcome";
        DialogueNode node2 = ScriptableObject.CreateInstance<DialogueNode>(); node2.name = "OldGuardDemo_02_Damage";
        DialogueNode node3 = ScriptableObject.CreateInstance<DialogueNode>(); node3.name = "OldGuardDemo_03_Obstacles";
        DialogueNode node4 = ScriptableObject.CreateInstance<DialogueNode>(); node4.name = "OldGuardDemo_04_FindLetter";
        DialogueNode node5 = ScriptableObject.CreateInstance<DialogueNode>(); node5.name = "OldGuardDemo_05_OpenPath";
        DialogueNode node6 = ScriptableObject.CreateInstance<DialogueNode>(); node6.name = "OldGuardDemo_06_Go";

        CreateAssetIfNeeded(node1, directoryPath);
        CreateAssetIfNeeded(node2, directoryPath);
        CreateAssetIfNeeded(node3, directoryPath);
        CreateAssetIfNeeded(node4, directoryPath);
        CreateAssetIfNeeded(node5, directoryPath);
        CreateAssetIfNeeded(node6, directoryPath);

        ConfigureNode(node1, "השומר הזקן גירסת דמו",
            "הגעתם אל יער הלחישות.",
            new DialogueChoice { choiceText = "המשך", nextNode = node2 });

        ConfigureNode(node2, "השומר הזקן גירסת דמו",
            "הנבל פגע בקסם של היער הזה, ועכשיו הדרך קדימה חסומה.",
            new DialogueChoice { choiceText = "המשך", nextNode = node3 });

        ConfigureNode(node3, "השומר הזקן גירסת דמו",
            "לאורך המסלול תמצאו מכשולים שיעצרו אתכם.",
            new DialogueChoice { choiceText = "המשך", nextNode = node4 });

        ConfigureNode(node4, "השומר הזקן גירסת דמו",
            "כדי לעבור אותם, תצטרכו למצוא את האות הנכונה ולהניח אותה במקום המתאים.",
            new DialogueChoice { choiceText = "המשך", nextNode = node5 });

        ConfigureNode(node5, "השומר הזקן גירסת דמו",
            "רק כך תוכלו לפתוח את הדרך ולהמשיך הלאה.",
            new DialogueChoice { choiceText = "המשך", nextNode = node6 });

        ConfigureNode(node6, "השומר הזקן גירסת דמו",
            "קדימה, צאו לדרך!",
            new DialogueChoice { choiceText = "סיום", nextNode = null });
        node6.actionType = DialogueActionType.CloseDialogue;

        SetDirty(node1, node2, node3, node4, node5, node6);
    }

    private static void GenerateOldGuardFullDialogue()
    {
        string directoryPath = "Assets/_OurAssets/ScriptableObjects/Dialogue/OldGuard_Full";
        EnsureDirectoryExists(directoryPath);

        DialogueNode node1 = ScriptableObject.CreateInstance<DialogueNode>(); node1.name = "OldGuardFull_01_Welcome";
        DialogueNode node2 = ScriptableObject.CreateInstance<DialogueNode>(); node2.name = "OldGuardFull_02_Damage";
        DialogueNode node3 = ScriptableObject.CreateInstance<DialogueNode>(); node3.name = "OldGuardFull_03_Obstacles";
        DialogueNode node4 = ScriptableObject.CreateInstance<DialogueNode>(); node4.name = "OldGuardFull_04_FindLetter";
        DialogueNode node5 = ScriptableObject.CreateInstance<DialogueNode>(); node5.name = "OldGuardFull_05_OpenPath";
        DialogueNode node6 = ScriptableObject.CreateInstance<DialogueNode>(); node6.name = "OldGuardFull_06_Museum";
        DialogueNode node7 = ScriptableObject.CreateInstance<DialogueNode>(); node7.name = "OldGuardFull_07_NextChallenge";

        CreateAssetIfNeeded(node1, directoryPath);
        CreateAssetIfNeeded(node2, directoryPath);
        CreateAssetIfNeeded(node3, directoryPath);
        CreateAssetIfNeeded(node4, directoryPath);
        CreateAssetIfNeeded(node5, directoryPath);
        CreateAssetIfNeeded(node6, directoryPath);
        CreateAssetIfNeeded(node7, directoryPath);

        ConfigureNode(node1, "השומר הזקן",
            "הגעתם אל יער הלחישות.",
            new DialogueChoice { choiceText = "המשך", nextNode = node2 });

        ConfigureNode(node2, "השומר הזקן",
            "הנבל פגע בקסם של היער הזה, ועכשיו הדרך קדימה חסומה.",
            new DialogueChoice { choiceText = "המשך", nextNode = node3 });

        ConfigureNode(node3, "השומר הזקן",
            "לאורך המסלול תמצאו מכשולים שיעצרו אתכם.",
            new DialogueChoice { choiceText = "המשך", nextNode = node4 });

        ConfigureNode(node4, "השומר הזקן",
            "כדי לעבור אותם, תצטרכו למצוא את האות הנכונה ולהניח אותה במקום המתאים.",
            new DialogueChoice { choiceText = "המשך", nextNode = node5 });

        ConfigureNode(node5, "השומר הזקן",
            "רק כך תוכלו לפתוח את הדרך ולהמשיך הלאה.",
            new DialogueChoice { choiceText = "המשך", nextNode = node6 });

        ConfigureNode(node6, "השומר הזקן",
            "בסוף המסלול מחכה לכם מוזיאון האותיות של היער.",
            new DialogueChoice { choiceText = "המשך", nextNode = node7 });

        ConfigureNode(node7, "השומר הזקן",
            "כשתגיעו אליו, האתגר הבא יתחיל.",
            new DialogueChoice { choiceText = "סיום", nextNode = null });
        node7.actionType = DialogueActionType.CloseDialogue;

        SetDirty(node1, node2, node3, node4, node5, node6, node7);
    }

    private static void GenerateRidingGuideDialogue()
    {
        string directoryPath = "Assets/_OurAssets/ScriptableObjects/Dialogue/RidingGuide";
        EnsureDirectoryExists(directoryPath);

        DialogueNode node1 = ScriptableObject.CreateInstance<DialogueNode>(); node1.name = "RidingGuide_01_Intro";
        DialogueNode node2 = ScriptableObject.CreateInstance<DialogueNode>(); node2.name = "RidingGuide_02_Bike";
        DialogueNode node3 = ScriptableObject.CreateInstance<DialogueNode>(); node3.name = "RidingGuide_03_Ride";
        DialogueNode node4 = ScriptableObject.CreateInstance<DialogueNode>(); node4.name = "RidingGuide_04_Control";
        DialogueNode node5 = ScriptableObject.CreateInstance<DialogueNode>(); node5.name = "RidingGuide_05_Go";

        CreateAssetIfNeeded(node1, directoryPath);
        CreateAssetIfNeeded(node2, directoryPath);
        CreateAssetIfNeeded(node3, directoryPath);
        CreateAssetIfNeeded(node4, directoryPath);
        CreateAssetIfNeeded(node5, directoryPath);

        ConfigureNode(node1, "מדריך הרכיבה",
            "היי, בדיוק בזמן.",
            new DialogueChoice { choiceText = "המשך", nextNode = node2 });

        ConfigureNode(node2, "מדריך הרכיבה",
            "האופנוע הזה ייקח אתכם אל היעד הבא.",
            new DialogueChoice { choiceText = "המשך", nextNode = node3 });

        ConfigureNode(node3, "מדריך הרכיבה",
            "תעלו עליו ותסעו קדימה לאורך הדרך.",
            new DialogueChoice { choiceText = "המשך", nextNode = node4 });

        ConfigureNode(node4, "מדריך הרכיבה",
            "תשמרו על שליטה ותמשיכו עד הסוף.",
            new DialogueChoice { choiceText = "המשך", nextNode = node5 });

        ConfigureNode(node5, "מדריך הרכיבה",
            " כשתגיעו לשם, המסע ימשיך. קדימה! תניעו וצאו לדרך.",
            new DialogueChoice { choiceText = "סיום", nextNode = null });
        node5.actionType = DialogueActionType.CloseDialogue;

        SetDirty(node1, node2, node3, node4, node5);
    }

    private static void GenerateLostCoinNPCDialogue()
    {
        string directoryPath = "Assets/_OurAssets/ScriptableObjects/Dialogue/LostCoinNPC";
        EnsureDirectoryExists(directoryPath);

        // ── Intro chain: NPC sends player to find the coin ──────────────────
        DialogueNode intro1 = ScriptableObject.CreateInstance<DialogueNode>(); intro1.name = "LostCoinNPC_Intro_01";
        DialogueNode intro2 = ScriptableObject.CreateInstance<DialogueNode>(); intro2.name = "LostCoinNPC_Intro_02";
        DialogueNode intro3 = ScriptableObject.CreateInstance<DialogueNode>(); intro3.name = "LostCoinNPC_Intro_03";

        CreateAssetIfNeeded(intro1, directoryPath);
        CreateAssetIfNeeded(intro2, directoryPath);
        CreateAssetIfNeeded(intro3, directoryPath);

        ConfigureNode(intro1, "הסוחר",
            "אוי לא! איבדתי את המטבע הזהב שלי כשהסתובבתי כאן קודם. הוא יקר לי מאוד!",
            new DialogueChoice { choiceText = "המשך", nextNode = intro2 });

        ConfigureNode(intro2, "הסוחר",
            "ראיתי אותו נופל מהכיס שלי לא רחוק מכאן. אם תמצאו אותו אני אהיה אסיר תודה!",
            new DialogueChoice { choiceText = "המשך", nextNode = intro3 });

        ConfigureNode(intro3, "הסוחר",
            "תוכלו לזהות אותו בקלות – יש קרן אור מיוחדת שמאירה מעליו. לכו, חפשו ותחזרו אליי!",
            new DialogueChoice { choiceText = "אצא לחפש", nextNode = null });
        intro3.actionType = DialogueActionType.CloseDialogue;

        // ── Return chain: player brings back the coin ───────────────────────
        DialogueNode ret1 = ScriptableObject.CreateInstance<DialogueNode>(); ret1.name = "LostCoinNPC_Return_01";
        DialogueNode ret2 = ScriptableObject.CreateInstance<DialogueNode>(); ret2.name = "LostCoinNPC_Return_02";

        CreateAssetIfNeeded(ret1, directoryPath);
        CreateAssetIfNeeded(ret2, directoryPath);

        ConfigureNode(ret1, "הסוחר",
            "מדהים! מצאתם את המטבע שלי! לא ידעתי מה אני עושה בלעדיו.",
            new DialogueChoice { choiceText = "המשך", nextNode = ret2 });

        ConfigureNode(ret2, "הסוחר",
            "קחו את זה כתגמול על עזרתכם. מגיע לכם!",
            new DialogueChoice { choiceText = "תודה", nextNode = null });
        ret2.actionType = DialogueActionType.CloseDialogue;

        SetDirty(intro1, intro2, intro3, ret1, ret2);
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }

    private static void SetDirty(params Object[] objects)
    {
        foreach (var obj in objects)
        {
            EditorUtility.SetDirty(obj);
        }
    }

    private static void CreateAssetIfNeeded(DialogueNode node, string path)
    {
        string fullPath = $"{path}/{node.name}.asset";
        // Check if exists to avoid overwrite error or handle update? 
        // CreateAsset will overwrite or fail? It throws if exists usually.
        // We deletes if exists to be clean.
        // Actually, AssetDatabase.CreateAsset works fine, but let's delete existing to be safe and clean.
        /*
        //if (File.Exists(fullPath)) {
            AssetDatabase.DeleteAsset(fullPath); 
        } 
        */
        // Actually, let's just use CreateAsset. If we want to support updating, we should load first.
        // But for a generator, overwriting is usually the goal.
        // AssetDatabase.CreateAsset replaces the file content if it can, but safest approach is:

        path = AssetDatabase.GenerateUniqueAssetPath(fullPath);
        AssetDatabase.CreateAsset(node, path);
    }

    private static void ConfigureNode(DialogueNode node, string speaker, string text, params DialogueChoice[] choices)
    {
        node.speakerName = speaker;
        node.dialogueText = text;
        node.choices = new List<DialogueChoice>(choices);
        node.actionType = DialogueActionType.None;
        node.actionParameter = "";
    }
}

#endif