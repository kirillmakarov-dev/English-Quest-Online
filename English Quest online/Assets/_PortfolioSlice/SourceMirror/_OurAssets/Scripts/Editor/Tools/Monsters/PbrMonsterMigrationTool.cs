#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// One-shot migration: RPGMonsterBundlePBR visuals + gameplay State controllers + renames.
/// Menu: Tools/English Kingdom/Monsters/Migrate To PBR Monsters
/// </summary>
public static class PbrMonsterMigrationTool
{
    private const string ControllersFolder = "Assets/_OurAssets/Animation/Animation Controller/Monsters";
    private const string DefinitionsFolder = "Assets/_OurAssets/Data/NPC/Monsters/Definitions";
    private const string BehavioursFolder = "Assets/_OurAssets/Data/NPC/Monsters";
    private const string PrefabsFolder = "Assets/_OurAssets/Art/Prefabs/Characters/Monsters";
    private const string LocalPrefabsFolder = PrefabsFolder + "/Local";
    private const string CatalogPath = "Assets/_OurAssets/Data/NPC/Monsters/MonsterCatalog.asset";
    private const string BaseNetworkedPath = PrefabsFolder + "/MonsterBase_Networked.prefab";
    private const string BaseLocalPath = LocalPrefabsFolder + "/MonsterBase_Local.prefab";

    private sealed class ClipSet
    {
        public string Idle;
        public string Walk;
        public string Run;
        public string Attack;
        public string Hit;
        public string Death;
    }

    private sealed class MonsterSpec
    {
        public string NewId;
        public string DisplayName;
        public string PrefabTypeName; // e.g. ChestMonster
        public string SourcePrefabPath;
        public string MeshFbxPath;
        public ClipSet Clips;
        public string OldDefinitionPath;
        public string OldNetworkedPrefabPath;
        public string OldLocalPrefabPath;
        public string OldBehaviourPath;
        public bool CreateNew;
        public string BehaviourCloneFromPath;
    }

    private static readonly MonsterSpec[] Specs =
    {
        new MonsterSpec
        {
            NewId = "mushroom_angry",
            DisplayName = "Mushroom Angry",
            PrefabTypeName = "MushroomAngry",
            SourcePrefabPath = "Assets/RPGMonsterBundlePBR/CommonStuffs/Prefab/Wave03/CharacterPBR/MushroomAngryPBRDefault.prefab",
            MeshFbxPath = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Mesh/Characters/Mushroom_Mesh.fbx",
            Clips = new ClipSet
            {
                Idle = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Mushroom/Mushroom_IdleNormalAngry.fbx",
                Walk = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Mushroom/Mushroom_walkFWDAngry.fbx",
                Run = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Mushroom/Mushroom_runFWDAngry.fbx",
                Attack = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Mushroom/Mushroom_Attack01Angry.fbx",
                Hit = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Mushroom/Mushroom_GetHitAngry.fbx",
                Death = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Mushroom/Mushroom_DieAngry.fbx"
            },
            OldDefinitionPath = DefinitionsFolder + "/Monster_CursedPriest_Definition.asset",
            OldNetworkedPrefabPath = PrefabsFolder + "/Monster_CursedPriest.prefab",
            OldLocalPrefabPath = LocalPrefabsFolder + "/Monster_CursedPriest_Local.prefab",
            OldBehaviourPath = BehavioursFolder + "/CursedPriest_Behaviour.asset"
        },
        new MonsterSpec
        {
            NewId = "chest_monster",
            DisplayName = "Chest Monster",
            PrefabTypeName = "ChestMonster",
            SourcePrefabPath = "Assets/RPGMonsterBundlePBR/CommonStuffs/Prefab/Wave02/CharacterPBR/ChestMonsterPBRDefault.prefab",
            MeshFbxPath = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Meshes/Character/ChestMonsterMesh.fbx",
            Clips = new ClipSet
            {
                Idle = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Chest Monster/IdleNormal.fbx",
                Walk = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Chest Monster/WalkFWD.fbx",
                Run = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Chest Monster/Run.fbx",
                Attack = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Chest Monster/Attack01.fbx",
                Hit = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Chest Monster/GetHit.fbx",
                Death = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Chest Monster/Die.fbx"
            },
            OldDefinitionPath = DefinitionsFolder + "/Monster_HulkBrute_Definition.asset",
            OldNetworkedPrefabPath = PrefabsFolder + "/Monster_HulkBrute.prefab",
            OldLocalPrefabPath = LocalPrefabsFolder + "/Monster_HulkBrute_Local.prefab",
            OldBehaviourPath = BehavioursFolder + "/HulkBrute_Behaviour.asset"
        },
        new MonsterSpec
        {
            NewId = "crab_monster",
            DisplayName = "Crab Monster",
            PrefabTypeName = "CrabMonster",
            SourcePrefabPath = "Assets/RPGMonsterBundlePBR/CommonStuffs/Prefab/Wave02/CharacterPBR/CrabMonsterPBRDefault.prefab",
            MeshFbxPath = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Meshes/Character/CrabMonsterMesh.fbx",
            Clips = new ClipSet
            {
                Idle = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Crab Monster/IdleNormal.fbx",
                Walk = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Crab Monster/WalkFWD.fbx",
                Run = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Crab Monster/Run.fbx",
                Attack = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Crab Monster/Attack01.fbx",
                Hit = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Crab Monster/GetHit.fbx",
                Death = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Crab Monster/Die.fbx"
            },
            OldDefinitionPath = DefinitionsFolder + "/Monster_InfectedRunner_Definition.asset",
            OldNetworkedPrefabPath = PrefabsFolder + "/Monster_InfectedRunner.prefab",
            OldLocalPrefabPath = LocalPrefabsFolder + "/Monster_InfectedRunner_Local.prefab",
            OldBehaviourPath = BehavioursFolder + "/InfectedRunner_Behaviour.asset"
        },
        new MonsterSpec
        {
            NewId = "rat_assassin",
            DisplayName = "Rat Assassin",
            PrefabTypeName = "RatAssassin",
            SourcePrefabPath = "Assets/RPGMonsterBundlePBR/CommonStuffs/Prefab/Wave02/CharacterPBR/RatAssassinPBRDefault.prefab",
            MeshFbxPath = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Meshes/Character/RatAssassinMesh.fbx",
            Clips = new ClipSet
            {
                Idle = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Rat Assassin/IdleNormal.fbx",
                Walk = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Rat Assassin/Walk.fbx",
                Run = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Rat Assassin/Run.fbx",
                Attack = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Rat Assassin/Attack01.fbx",
                Hit = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Rat Assassin/GetHit.fbx",
                Death = "Assets/RPGMonsterBundlePBR/RPGMonsterWave02PBR/Animations/Rat Assassin/Die.fbx"
            },
            OldDefinitionPath = DefinitionsFolder + "/Monster_PoliceEnforcer_Definition.asset",
            OldNetworkedPrefabPath = PrefabsFolder + "/Monster_PoliceEnforcer.prefab",
            OldLocalPrefabPath = LocalPrefabsFolder + "/Monster_PoliceEnforcer_Local.prefab",
            OldBehaviourPath = BehavioursFolder + "/PoliceEnforcer_Behaviour.asset"
        },
        new MonsterSpec
        {
            NewId = "cactus",
            DisplayName = "Cactus",
            PrefabTypeName = "Cactus",
            SourcePrefabPath = "Assets/RPGMonsterBundlePBR/CommonStuffs/Prefab/Wave03/CharacterPBR/CactusPBRDefault.prefab",
            MeshFbxPath = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Mesh/Characters/CactusMesh.fbx",
            Clips = new ClipSet
            {
                Idle = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Cactus/Cactus_IdleNormal.fbx",
                Walk = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Cactus/Cactus_WalkFWD.fbx",
                Run = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Cactus/Cactus_RunFWD.fbx",
                Attack = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Cactus/Cactus_Attack01.fbx",
                Hit = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Cactus/Cactus_GetHit.fbx",
                Death = "Assets/RPGMonsterBundlePBR/RPGMonsterWave03PBR/Animation/Cactus/Cactus_Die.fbx"
            },
            OldDefinitionPath = DefinitionsFolder + "/MonsterPlaceholder_Definition.asset",
            OldNetworkedPrefabPath = PrefabsFolder + "/MonsterPlaceholder.prefab",
            OldLocalPrefabPath = LocalPrefabsFolder + "/MonsterPlaceholder_Local.prefab",
            OldBehaviourPath = null,
            BehaviourCloneFromPath = BehavioursFolder + "/SoldierGrunt_Behaviour.asset"
        },
        new MonsterSpec
        {
            NewId = "spider",
            DisplayName = "Spider",
            PrefabTypeName = "Spider",
            SourcePrefabPath = "Assets/RPGMonsterBundlePBR/CommonStuffs/Prefab/Wave01/CharacterPBR/SpiderPBRDefault.prefab",
            MeshFbxPath = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Meshes/Character/SpiderMesh.fbx",
            Clips = new ClipSet
            {
                Idle = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/Spider/IdleNormal_Spider_Anim.fbx",
                Walk = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/Spider/Walk_Spider_Anim.fbx",
                Run = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/Spider/Run_Spider_Anim.fbx",
                Attack = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/Spider/Attack01_Spider_Anim.fbx",
                Hit = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/Spider/GetHit_Spider_Anim.fbx",
                Death = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/Spider/Die_Spider_Anim.fbx"
            },
            OldDefinitionPath = DefinitionsFolder + "/Monster_SoldierGrunt_Definition.asset",
            OldNetworkedPrefabPath = PrefabsFolder + "/Monster_SoldierGrunt.prefab",
            OldLocalPrefabPath = LocalPrefabsFolder + "/Monster_SoldierGrunt_Local.prefab",
            OldBehaviourPath = BehavioursFolder + "/SoldierGrunt_Behaviour.asset"
        },
        new MonsterSpec
        {
            NewId = "turtle_shell",
            DisplayName = "Turtle Shell",
            PrefabTypeName = "TurtleShell",
            SourcePrefabPath = "Assets/RPGMonsterBundlePBR/CommonStuffs/Prefab/Wave01/CharacterPBR/TurtleShellPBR.prefab",
            MeshFbxPath = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Meshes/Character/TurtleShellMesh.fbx",
            Clips = new ClipSet
            {
                Idle = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/TurtleShell/IdleNormal_TurtleShell_Anim.fbx",
                Walk = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/TurtleShell/Walk_TurtleShell_Anim.fbx",
                Run = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/TurtleShell/Run_TurtleShell_Anim.fbx",
                Attack = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/TurtleShell/Attack01_TurtleShell_Anim.fbx",
                Hit = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/TurtleShell/GetHit_TurtleShell_Anim.fbx",
                Death = "Assets/RPGMonsterBundlePBR/RPGMonsterWave01PBR/Animations/TurtleShell/Die_TurtleShell_Anim.fbx"
            },
            CreateNew = true,
            BehaviourCloneFromPath = BehavioursFolder + "/HulkBrute_Behaviour.asset"
        }
    };

    [MenuItem("Tools/English Kingdom/Monsters/Migrate To PBR Monsters")]
    public static void MigrateFromMenu()
    {
        try
        {
            string report = MigrateAll();
            Debug.Log($"[PbrMonsterMigrationTool] Done.\n{report}");
            EditorUtility.DisplayDialog("PBR Monster Migration", report, "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PbrMonsterMigrationTool] Failed: {ex}");
            EditorUtility.DisplayDialog("PBR Monster Migration Failed", ex.Message, "OK");
            throw;
        }
    }

    /// <summary>Batch / bridge entry point.</summary>
    public static string MigrateAll()
    {
        EnsureFolder(ControllersFolder);

        var lines = new List<string>();
        var controllers = new Dictionary<string, AnimatorController>();
        var definitions = new List<MonsterDefinitionSO>();

        foreach (MonsterSpec spec in Specs)
        {
            AnimatorController controller = CreateOrUpdateController(spec);
            controllers[spec.NewId] = controller;
            lines.Add($"Controller: {controller.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        foreach (MonsterSpec spec in Specs)
        {
            AnimatorController controller = controllers[spec.NewId];
            Avatar avatar = LoadAvatar(spec.MeshFbxPath);
            if (avatar == null)
                throw new InvalidOperationException($"Missing avatar on mesh FBX: {spec.MeshFbxPath}");

            MonsterBehaviorConfigSO behaviour = EnsureBehaviour(spec, controller, out string behaviourNote);
            lines.Add(behaviourNote);

            string networkedPath;
            string localPath;
            MonsterDefinitionSO definition;

            if (spec.CreateNew)
            {
                networkedPath = PrefabsFolder + $"/Monster_{spec.PrefabTypeName}.prefab";
                localPath = LocalPrefabsFolder + $"/Monster_{spec.PrefabTypeName}_Local.prefab";
                CreatePrefabVariantFromBase(BaseNetworkedPath, networkedPath);
                CreatePrefabVariantFromBase(BaseLocalPath, localPath);
                ApplyVisual(networkedPath, spec.SourcePrefabPath, controller, avatar, behaviour);
                ApplyVisual(localPath, spec.SourcePrefabPath, controller, avatar, behaviour);

                HealthStatsSO health = EnsureHealthAsset(spec);
                string defPath = DefinitionsFolder + $"/Monster_{spec.PrefabTypeName}_Definition.asset";
                definition = CreateDefinitionAsset(defPath, spec, behaviour, health, networkedPath, localPath);
            }
            else
            {
                ApplyVisual(spec.OldNetworkedPrefabPath, spec.SourcePrefabPath, controller, avatar, behaviour);
                ApplyVisual(spec.OldLocalPrefabPath, spec.SourcePrefabPath, controller, avatar, behaviour);

                networkedPath = RenameAssetKeepGuid(
                    spec.OldNetworkedPrefabPath,
                    PrefabsFolder + $"/Monster_{spec.PrefabTypeName}.prefab");
                localPath = RenameAssetKeepGuid(
                    spec.OldLocalPrefabPath,
                    LocalPrefabsFolder + $"/Monster_{spec.PrefabTypeName}_Local.prefab");

                if (!string.IsNullOrEmpty(spec.OldBehaviourPath) &&
                    AssetDatabase.LoadAssetAtPath<MonsterBehaviorConfigSO>(spec.OldBehaviourPath) != null)
                {
                    string newBehaviourPath = BehavioursFolder + $"/{spec.PrefabTypeName}_Behaviour.asset";
                    RenameAssetKeepGuid(spec.OldBehaviourPath, newBehaviourPath);
                    behaviour = AssetDatabase.LoadAssetAtPath<MonsterBehaviorConfigSO>(newBehaviourPath);
                }

                string newDefPath = DefinitionsFolder + $"/Monster_{spec.PrefabTypeName}_Definition.asset";
                string defPath = RenameAssetKeepGuid(spec.OldDefinitionPath, newDefPath);
                definition = AssetDatabase.LoadAssetAtPath<MonsterDefinitionSO>(defPath);
                HealthStatsSO health = EnsureHealthAsset(spec, definition);
                WriteDefinition(definition, spec, behaviour, health, networkedPath, localPath);
            }

            definitions.Add(definition);
            lines.Add($"Prefab+Def: {spec.NewId} → {definition.name}");
        }

        UpdateCatalog(definitions);
        lines.Add($"Catalog entries: {definitions.Count}");

        // Single-monster spawn pools: rename asset files to match new types where possible.
        RenameSpawnPoolAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return string.Join("\n", lines);
    }

    private static AnimatorController CreateOrUpdateController(MonsterSpec spec)
    {
        string path = $"{ControllersFolder}/{spec.PrefabTypeName}.controller";
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("State", AnimatorControllerParameterType.Int);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        // Clear default empty state Unity creates.
        while (sm.states.Length > 0)
            sm.RemoveState(sm.states[0].state);

        AnimationClip idle = LoadClip(spec.Clips.Idle);
        AnimationClip walk = LoadClip(spec.Clips.Walk);
        AnimationClip run = LoadClip(spec.Clips.Run);
        AnimationClip attack = LoadClip(spec.Clips.Attack);
        AnimationClip hit = LoadClip(spec.Clips.Hit);
        AnimationClip death = LoadClip(spec.Clips.Death);

        AnimatorState idleState = AddState(sm, "Idle", idle, new Vector3(300, 120, 0));
        AnimatorState walkState = AddState(sm, "Walk", walk, new Vector3(300, 0, 0));
        AnimatorState runState = AddState(sm, "Run", run, new Vector3(500, 120, 0));
        AnimatorState attackState = AddState(sm, "Attack", attack, new Vector3(500, 0, 0));
        AnimatorState hitState = AddState(sm, "Hit", hit, new Vector3(700, 120, 0));
        AnimatorState deathState = AddState(sm, "Death", death, new Vector3(700, 0, 0));
        sm.defaultState = idleState;

        AddAnyTransition(sm, idleState, 0, 0.2f);
        AddAnyTransition(sm, walkState, 1, 0.2f);
        AddAnyTransition(sm, runState, 2, 0.15f);
        AddAnyTransition(sm, attackState, 3, 0.1f);
        AddAnyTransition(sm, hitState, 4, 0.05f);
        AddAnyTransition(sm, deathState, 5, 0.1f);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip, Vector3 pos)
    {
        AnimatorState state = sm.AddState(name, pos);
        state.motion = clip;
        state.writeDefaultValues = true;
        return state;
    }

    private static void AddAnyTransition(AnimatorStateMachine sm, AnimatorState dst, int stateValue, float duration)
    {
        AnimatorStateTransition t = sm.AddAnyStateTransition(dst);
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.duration = duration;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.Equals, stateValue, "State");
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        AnimationClip best = null;
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
            {
                best = clip;
                break;
            }
        }

        if (best == null)
            throw new InvalidOperationException($"No AnimationClip in '{fbxPath}'");
        return best;
    }

    private static Avatar LoadAvatar(string meshFbxPath)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(meshFbxPath);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Avatar avatar)
                return avatar;
        }

        // Fallback: avatar often lives on the character prefab's Animator.
        return null;
    }

    private static Avatar LoadAvatarFromSourcePrefab(string sourcePrefabPath)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
        if (source == null)
            return null;
        Animator anim = source.GetComponentInChildren<Animator>(true);
        return anim != null ? anim.avatar : null;
    }

    private static MonsterBehaviorConfigSO EnsureBehaviour(
        MonsterSpec spec,
        AnimatorController controller,
        out string note)
    {
        MonsterBehaviorConfigSO behaviour = null;
        string targetPath = BehavioursFolder + $"/{spec.PrefabTypeName}_Behaviour.asset";

        if (!string.IsNullOrEmpty(spec.OldBehaviourPath))
            behaviour = AssetDatabase.LoadAssetAtPath<MonsterBehaviorConfigSO>(spec.OldBehaviourPath);

        if (behaviour == null && File.Exists(Absolute(targetPath)))
            behaviour = AssetDatabase.LoadAssetAtPath<MonsterBehaviorConfigSO>(targetPath);

        if (behaviour == null && !string.IsNullOrEmpty(spec.BehaviourCloneFromPath))
        {
            string cloneFrom = spec.BehaviourCloneFromPath;
            // After earlier renames in same run, Hulk may already be ChestMonster.
            if (AssetDatabase.LoadAssetAtPath<MonsterBehaviorConfigSO>(cloneFrom) == null &&
                cloneFrom.Contains("HulkBrute"))
            {
                cloneFrom = BehavioursFolder + "/ChestMonster_Behaviour.asset";
            }

            if (AssetDatabase.LoadAssetAtPath<MonsterBehaviorConfigSO>(cloneFrom) == null &&
                cloneFrom.Contains("SoldierGrunt"))
            {
                cloneFrom = BehavioursFolder + "/Spider_Behaviour.asset";
            }

            if (!AssetDatabase.CopyAsset(cloneFrom, targetPath))
                throw new InvalidOperationException($"Failed to clone behaviour from {cloneFrom} to {targetPath}");
            behaviour = AssetDatabase.LoadAssetAtPath<MonsterBehaviorConfigSO>(targetPath);
            note = $"Behaviour cloned: {targetPath}";
        }
        else if (behaviour == null)
        {
            behaviour = ScriptableObject.CreateInstance<MonsterBehaviorConfigSO>();
            AssetDatabase.CreateAsset(behaviour, targetPath);
            note = $"Behaviour created: {targetPath}";
        }
        else
        {
            note = $"Behaviour reused: {behaviour.name}";
        }

        ApplyClipDurations(behaviour, spec);
        behaviour.animatorStateName = "State";
        behaviour.attackAnimStateName = "Attack";
        behaviour.idleState = 0;
        behaviour.walkState = 1;
        // Keep runState as configured; default to 2 for new plant-likes if still pointing at walk-only hulk remap
        if (behaviour.runState == 1 && (spec.NewId == "chest_monster" || spec.NewId == "turtle_shell"))
            behaviour.runState = 1; // heavy/slow: walk clip for chase is fine
        else if (behaviour.runState != 1)
            behaviour.runState = 2;
        else
            behaviour.runState = 2;

        // Chest/turtle stay slower: use walk as run like old hulk.
        if (spec.NewId is "chest_monster" or "turtle_shell")
            behaviour.runState = 1;

        EditorUtility.SetDirty(behaviour);
        return behaviour;
    }

    private static void ApplyClipDurations(MonsterBehaviorConfigSO behaviour, MonsterSpec spec)
    {
        AnimationClip hit = LoadClip(spec.Clips.Hit);
        AnimationClip death = LoadClip(spec.Clips.Death);
        if (hit != null && hit.length > 0.05f)
            behaviour.hitAnimDuration = hit.length;
        if (death != null && death.length > 0.05f)
            behaviour.deathAnimDuration = death.length;
    }

    private static void ApplyVisual(
        string prefabPath,
        string sourcePrefabPath,
        AnimatorController controller,
        Avatar avatar,
        MonsterBehaviorConfigSO behaviour)
    {
        if (avatar == null)
            avatar = LoadAvatarFromSourcePrefab(sourcePrefabPath);
        if (avatar == null)
            throw new InvalidOperationException($"No avatar for '{prefabPath}' from '{sourcePrefabPath}'");

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform visual = FindChild(root.transform, "Visual");
            if (visual == null)
                throw new InvalidOperationException($"No Visual child on '{prefabPath}'");

            // Remove old mesh/skeleton under Visual.
            for (int i = visual.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(visual.GetChild(i).gameObject);

            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
            if (sourcePrefab == null)
                throw new InvalidOperationException($"Missing source prefab '{sourcePrefabPath}'");

            GameObject sourceInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            // Unpack so we own the hierarchy inside our monster prefab.
            PrefabUtility.UnpackPrefabInstance(sourceInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // Move children onto Visual; strip nested Animator.
            Animator nestedAnim = sourceInstance.GetComponent<Animator>();
            if (nestedAnim != null)
                UnityEngine.Object.DestroyImmediate(nestedAnim);

            while (sourceInstance.transform.childCount > 0)
            {
                Transform child = sourceInstance.transform.GetChild(0);
                child.SetParent(visual, false);
            }

            UnityEngine.Object.DestroyImmediate(sourceInstance);

            Animator visualAnimator = visual.GetComponent<Animator>();
            if (visualAnimator == null)
                visualAnimator = visual.gameObject.AddComponent<Animator>();

            visualAnimator.avatar = avatar;
            visualAnimator.runtimeAnimatorController = controller;
            visualAnimator.applyRootMotion = false;
            visualAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // Rebind AI / LOD refs to this Visual + Animator.
            MonsterAIComponent ai = root.GetComponent<MonsterAIComponent>();
            if (ai != null)
            {
                SerializedObject so = new SerializedObject(ai);
                so.FindProperty("_animator").objectReferenceValue = visualAnimator;
                SerializedProperty configProp = so.FindProperty("_config");
                if (configProp != null && behaviour != null)
                    configProp.objectReferenceValue = behaviour;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            NPCVisualLODController lod = root.GetComponent<NPCVisualLODController>();
            if (lod != null)
            {
                SerializedObject so = new SerializedObject(lod);
                so.FindProperty("_visualRoot").objectReferenceValue = visual;
                so.FindProperty("_animator").objectReferenceValue = visualAnimator;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Optional nametag / floating UI root also uses _visualRoot on other comps.
            foreach (MonoBehaviour mb in root.GetComponents<MonoBehaviour>())
            {
                if (mb == null || mb is MonsterAIComponent || mb is NPCVisualLODController)
                    continue;
                SerializedObject so = new SerializedObject(mb);
                SerializedProperty visualRoot = so.FindProperty("_visualRoot");
                if (visualRoot != null && visualRoot.propertyType == SerializedPropertyType.ObjectReference)
                {
                    visualRoot.objectReferenceValue = visual;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void CreatePrefabVariantFromBase(string basePath, string newPath)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(newPath) != null)
            return;

        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
        if (basePrefab == null)
            throw new InvalidOperationException($"Missing base prefab '{basePath}'");

        string dir = Path.GetDirectoryName(newPath)?.Replace('\\', '/');
        EnsureFolder(dir);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        PrefabUtility.SaveAsPrefabAsset(instance, newPath);
        UnityEngine.Object.DestroyImmediate(instance);
    }

    private static HealthStatsSO EnsureHealthAsset(MonsterSpec spec, MonsterDefinitionSO existingDef = null)
    {
        const string healthFolder = "Assets/_OurAssets/Data/Health/Monsters";
        EnsureFolder(healthFolder);
        string targetPath = $"{healthFolder}/{spec.PrefabTypeName}_Health.asset";

        if (existingDef != null)
        {
            SerializedObject so = new SerializedObject(existingDef);
            HealthStatsSO existing = so.FindProperty("_health").objectReferenceValue as HealthStatsSO;
            if (existing != null)
            {
                string existingPath = AssetDatabase.GetAssetPath(existing);
                if (!string.IsNullOrEmpty(existingPath) && existingPath != targetPath &&
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath) == null)
                {
                    RenameAssetKeepGuid(existingPath, targetPath);
                    return AssetDatabase.LoadAssetAtPath<HealthStatsSO>(targetPath);
                }

                return existing;
            }
        }

        HealthStatsSO already = AssetDatabase.LoadAssetAtPath<HealthStatsSO>(targetPath);
        if (already != null)
            return already;

        string[] cloneCandidates =
        {
            $"{healthFolder}/HulkBrute_Health.asset",
            $"{healthFolder}/ChestMonster_Health.asset",
            $"{healthFolder}/SoldierGrunt_Health.asset",
            $"{healthFolder}/Spider_Health.asset"
        };

        foreach (string candidate in cloneCandidates)
        {
            if (AssetDatabase.LoadAssetAtPath<HealthStatsSO>(candidate) == null)
                continue;
            if (!AssetDatabase.CopyAsset(candidate, targetPath))
                continue;
            return AssetDatabase.LoadAssetAtPath<HealthStatsSO>(targetPath);
        }

        throw new InvalidOperationException($"Could not create health asset for {spec.PrefabTypeName}");
    }

    private static MonsterDefinitionSO CreateDefinitionAsset(
        string path,
        MonsterSpec spec,
        MonsterBehaviorConfigSO behaviour,
        HealthStatsSO health,
        string networkedPath,
        string localPath)
    {
        MonsterDefinitionSO def = AssetDatabase.LoadAssetAtPath<MonsterDefinitionSO>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<MonsterDefinitionSO>();
            AssetDatabase.CreateAsset(def, path);
        }

        WriteDefinition(def, spec, behaviour, health, networkedPath, localPath);
        return def;
    }

    private static void WriteDefinition(
        MonsterDefinitionSO def,
        MonsterSpec spec,
        MonsterBehaviorConfigSO behaviour,
        HealthStatsSO health,
        string networkedPath,
        string localPath)
    {
        SerializedObject so = new SerializedObject(def);
        so.FindProperty("_monsterId").stringValue = spec.NewId;
        so.FindProperty("_displayName").stringValue = spec.DisplayName;
        so.FindProperty("_behaviour").objectReferenceValue = behaviour;
        if (health != null)
            so.FindProperty("_health").objectReferenceValue = health;
        so.FindProperty("_networkedPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(networkedPath);
        so.FindProperty("_localPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(localPath);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(def);
    }

    private static void UpdateCatalog(List<MonsterDefinitionSO> definitions)
    {
        MonsterCatalogSO catalog = AssetDatabase.LoadAssetAtPath<MonsterCatalogSO>(CatalogPath);
        if (catalog == null)
            throw new InvalidOperationException($"Missing catalog at {CatalogPath}");

        SerializedObject so = new SerializedObject(catalog);
        SerializedProperty list = so.FindProperty("_monsters");
        list.ClearArray();
        for (int i = 0; i < definitions.Count; i++)
        {
            list.InsertArrayElementAtIndex(i);
            list.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static void RenameSpawnPoolAssets()
    {
        var renames = new (string from, string to)[]
        {
            ("Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolSoldierGrunt.asset",
                "Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolSpider.asset"),
            ("Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolPoliceEnforcer.asset",
                "Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolRatAssassin.asset"),
            ("Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolInfectedRunner.asset",
                "Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolCrabMonster.asset"),
            ("Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolHulkBrute.asset",
                "Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolChestMonster.asset"),
            ("Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnCursedPriest.asset",
                "Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolMushroomAngry.asset"),
            ("Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolPlaceHolder.asset",
                "Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolCactus.asset"),
        };

        foreach ((string from, string to) in renames)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(from) != null)
                RenameAssetKeepGuid(from, to);
        }

        // Add turtle single-monster pool if missing.
        string turtlePool = "Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolTurtleShell.asset";
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(turtlePool) == null)
        {
            string template = "Assets/_OurAssets/Data/NPC/Monsters/SpawnPools/SingleMonster/MonsterSpawnPoolChestMonster.asset";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(template) != null)
            {
                AssetDatabase.CopyAsset(template, turtlePool);
                MonsterSpawnPoolSO pool = AssetDatabase.LoadAssetAtPath<MonsterSpawnPoolSO>(turtlePool);
                MonsterDefinitionSO turtleDef = AssetDatabase.LoadAssetAtPath<MonsterDefinitionSO>(
                    DefinitionsFolder + "/Monster_TurtleShell_Definition.asset");
                if (pool != null && turtleDef != null)
                {
                    SerializedObject so = new SerializedObject(pool);
                    SerializedProperty entries = so.FindProperty("_entries");
                    entries.ClearArray();
                    entries.InsertArrayElementAtIndex(0);
                    SerializedProperty entry = entries.GetArrayElementAtIndex(0);
                    entry.FindPropertyRelative("monster").objectReferenceValue = turtleDef;
                    entry.FindPropertyRelative("weight").intValue = 1;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(pool);
                }
            }
        }
    }

    private static string RenameAssetKeepGuid(string fromPath, string toPath)
    {
        if (fromPath == toPath)
            return toPath;

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(toPath) != null)
            return toPath;

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fromPath) == null)
            return toPath;

        string error = AssetDatabase.MoveAsset(fromPath, toPath);
        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException($"MoveAsset failed '{fromPath}' → '{toPath}': {error}");
        return toPath;
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string leaf = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static string Absolute(string assetPath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(projectRoot ?? "", assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
#endif
