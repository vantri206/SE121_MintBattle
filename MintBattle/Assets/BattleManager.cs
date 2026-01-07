using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public enum BattleState { WaitingForPlayers, PlayerTurn, SelectingSkill, SelectingTarget, EnemyTurn, Busy, Win, Lose }

public class BattleSystem : NetworkBehaviour
{
    public static BattleSystem Instance;

    [Header("Network References")]
    public NetworkObject heroNetPrefab;
    public Transform[] heroSpawnPoints;
    public Transform[] enemySpawnPoints;
    public BattleHUD battleHUD;

    [Header("Visual Helpers")]
    public GameObject selectionMarkerPrefab;
    private GameObject currentMarker;

    [Header("Network State")]
    [Networked] public int PlayersReadyCount { get; set; } = 0;

    public BattleState localState;

    public List<BattleUnit> allUnits = new List<BattleUnit>();

    private int currentSkillIndex = 0;
    private List<RuntimeSkill> currentActiveSkills;
    private RuntimeSkill selectedSkill;
    private List<BattleUnit> validTargets = new List<BattleUnit>();
    private int currentTargetIndex = 0;

    private SignatureService sigService;
    public int PendingRewardId { get; private set; }
    public string PendingSignature { get; private set; }
    public string MyWalletId => PlayerProfile.Instance.WalletAddress;
    public string MyName => PlayerProfile.Instance.PlayerName;

    public ItemDropManager itemDropManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        sigService = FindFirstObjectByType<SignatureService>();
    }

    public override void Spawned()
    {
        localState = BattleState.WaitingForPlayers;
        RPC_RegisterPlayerInfo(PlayerProfile.Instance.WalletAddress, PlayerProfile.Instance.PlayerName);
        StartCoroutine(SpawnMyTeamRoutine());
    }

    IEnumerator SpawnMyTeamRoutine()
    {
        yield return null;
        List<string> myTeamIds = TeamManager.Instance.GetHeroesInTeam(TeamManager.Instance.CurrentBattleTeamIndex).ToList();
        string myWallet = MyWalletId;
        string myName = MyName;

        int index = 0;
        foreach (string heroId in myTeamIds)
        {
            Hero hero = PlayerInventory.Instance.GetHeroById(heroId);
            if (hero != null)
            {
                int currentIndex = index;

                Vector3 spawnPos = Vector3.zero;
                if (Object.HasStateAuthority) spawnPos = heroSpawnPoints[index].position;
                else spawnPos = enemySpawnPoints[index].position;

                NetworkObject no = Runner.Spawn(heroNetPrefab, spawnPos, Quaternion.identity, Runner.LocalPlayer,
                    onBeforeSpawned: (runner, obj) =>
                    {
                        BattleUnit unit = obj.GetComponent<BattleUnit>();
                        if (unit != null)
                        {
                            unit.NetworkSetup(myWallet, myName, hero, currentIndex);
                        }
                    }
                );
                index++;
            }
        }
        RPC_PlayerReady();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_RegisterPlayerInfo(string walletId, string playerName)
    {
        if (battleHUD != null)
        {
            bool isMe = (walletId == MyWalletId);
            battleHUD.UpdatePlayerName(isMe, playerName);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayerReady()
    {
        PlayersReadyCount++;
        if (PlayersReadyCount >= 2)
        {
            RPC_InitialSortAndStart();
        }
    }

    public void RegisterUnit(BattleUnit unit)
    {
        allUnits.Add(unit);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_InitialSortAndStart()
    {
        SortUnits();

        if (Object.HasStateAuthority)
        {
            RPC_StartTurn();
        }
    }

    void SortUnits()
    {
        allUnits.Sort((a, b) =>
        {
            int speedA = a.Speed;
            int speedB = b.Speed;
            if (speedA != speedB)
            {
                return speedB.CompareTo(speedA);
            }

            return a.Object.Id.ToString().CompareTo(b.Object.Id.ToString());
        });

        string order = "Turn Order: ";
        foreach (var u in allUnits) order += $"{u.unitName}({u.Speed}) > ";
        Debug.Log(order);

        if (allUnits.Count > 0)
            battleHUD.UpdateTurnOrderBar(allUnits, allUnits[0]);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartTurn()
    {
        if (allUnits.Count == 0) return;

        BattleUnit currentUnit = allUnits[0];

        if (currentUnit.CurrentHP <= 0)
        {
            MoveCurrentUnitToBack();
            if (Object.HasStateAuthority) RPC_StartTurn();
            return;
        }

        currentUnit.OnTurnStart();

        battleHUD.UpdateHeroStatus(currentUnit);

        bool isMyTurn = (currentUnit.OwnerId == MyWalletId);
        battleHUD.UpdateTurnIndicator(isMyTurn);

        battleHUD.UpdateTurnOrderBar(allUnits, currentUnit);

        if (currentUnit.OwnerId == MyWalletId)
        {
            localState = BattleState.SelectingSkill;
            currentSkillIndex = 0;
            currentActiveSkills = currentUnit.ActiveSkills;

            battleHUD.EnableActions(true);
            battleHUD.SetupSkillButtons(currentActiveSkills);
            battleHUD.UpdateSkillSelectionUI(currentSkillIndex);
        }
        else
        {
            localState = BattleState.EnemyTurn;
            battleHUD.EnableActions(false);
            if (currentMarker) currentMarker.SetActive(false);
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_EndTurn()
    {
        MoveCurrentUnitToBack();
        if (Object.HasStateAuthority)
        {
            if (!CheckBattleOver())
            {
                RPC_StartTurn();
            }
        }
    }
    private void Update()
    {
        if (localState == BattleState.SelectingSkill)
        {
            HandleSkillSelectionInput();
        }
        else if (localState == BattleState.SelectingTarget)
        {
            HandleTargetSelectionInput();
        }
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayerSurrender(NetworkString<_64> looserWalletId)
    {
        Debug.Log($"Player {looserWalletId} wants to surrender!");

        string winnerWalletId = "";

        foreach (var unit in allUnits)
        {
            if (unit.OwnerId.ToString() != looserWalletId.ToString())
            {
                winnerWalletId = unit.OwnerId.ToString();
                break;
            }
        }

        if (!string.IsNullOrEmpty(winnerWalletId))
        {
            Authority_FinishMatch(winnerWalletId);
        }
    }
    void HandleSkillSelectionInput()
    {
        if (currentActiveSkills == null || currentActiveSkills.Count == 0) return;

        bool hasInput = false;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            currentSkillIndex++;
            if (currentSkillIndex >= currentActiveSkills.Count) currentSkillIndex = 0;
            hasInput = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            currentSkillIndex--;
            if (currentSkillIndex < 0) currentSkillIndex = currentActiveSkills.Count - 1;
            hasInput = true;
        }

        if (hasInput)
        {
            battleHUD.UpdateSkillSelectionUI(currentSkillIndex);
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnSkillSelected(currentActiveSkills[currentSkillIndex]);
        }
    }

    public void OnSkillSelected(RuntimeSkill skill)
    {
        if (localState != BattleState.SelectingSkill) return;

        selectedSkill = skill;

        if (skill.currentCooldown > 0) return;

        switch (skill.skillData.TargetType)
        {
            case SkillTargetType.SingleEnemy:
            case SkillTargetType.Ally:
                StartTargetSelectionMode(skill.skillData.TargetType);
                break;

            case SkillTargetType.AllEnemies:
                SendAttackRequest(allUnits[0], null, selectedSkill);
                break;
        }
    }

    void StartTargetSelectionMode(SkillTargetType type)
    {
        localState = BattleState.SelectingTarget;
        battleHUD.EnableActions(false);

        validTargets.Clear();
        string myId = MyWalletId;

        if (type == SkillTargetType.SingleEnemy)
        {
            validTargets = allUnits.Where(u => u.OwnerId != myId && u.CurrentHP > 0).ToList();
        }
        else if (type == SkillTargetType.Ally)
        {
            validTargets = allUnits.Where(u => u.OwnerId == myId && u.CurrentHP > 0).ToList();
        }

        validTargets = validTargets.OrderBy(u => u.transform.position.x).ToList();

        if (validTargets.Count > 0)
        {
            currentTargetIndex = 0;
            UpdateMarkerPosition();
        }
        else
        {
            CancelSelection();
        }
    }

    void HandleTargetSelectionInput()
    {
        if (validTargets.Count == 0) return;

        bool hasInput = false;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            currentTargetIndex++;
            if (currentTargetIndex >= validTargets.Count) currentTargetIndex = 0;
            hasInput = true;
        }

        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            currentTargetIndex--;
            if (currentTargetIndex < 0) currentTargetIndex = validTargets.Count - 1;
            hasInput = true;
        }

        if (hasInput)
        {
            UpdateMarkerPosition();
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmTargetSelection();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelSelection();
        }
    }

    void UpdateMarkerPosition()
    {
        BattleUnit target = validTargets[currentTargetIndex];

        if (currentMarker == null && selectionMarkerPrefab != null)
        {
            currentMarker = Instantiate(selectionMarkerPrefab);
        }

        if (currentMarker != null)
        {
            currentMarker.SetActive(true);
            Transform markerPos = target.transform.Find("VisualRoot/HeroUnitVisual/MarkerPosition");
            if (markerPos == null) markerPos = target.transform;
            currentMarker.transform.position = markerPos.position;
        }
    }

    void CancelSelection()
    {
        if (currentMarker) currentMarker.SetActive(false);
        localState = BattleState.SelectingSkill;
        battleHUD.EnableActions(true);
        battleHUD.UpdateSkillSelectionUI(currentSkillIndex);
    }

    void ConfirmTargetSelection()
    {
        BattleUnit target = validTargets[currentTargetIndex];
        if (currentMarker) currentMarker.SetActive(false);

        SendAttackRequest(allUnits[0], target, selectedSkill);
    }

    public void SendAttackRequest(BattleUnit attacker, BattleUnit target, RuntimeSkill skill)
    {
        localState = BattleState.Busy;
        NetworkId targetNetId = target != null ? target.Object.Id : default;

        RPC_RequestPerformAction(attacker.Object.Id, targetNetId, skill.skillData.Id);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncDamageOrHeal(NetworkId targetId, int value, bool isHeal)
    {
        if (Runner.TryFindObject(targetId, out NetworkObject targetObj))
        {
            BattleUnit unit = targetObj.GetComponent<BattleUnit>();
            if (unit != null)
            {
                if (unit.Object.HasStateAuthority)
                {
                    if (isHeal)
                    {
                        unit.Heal(value);
                    }
                    else
                    {
                        unit.TakeDamage(value);
                    }
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestPerformAction(NetworkId attackerId, NetworkId targetId, string skillId)
    {
        RPC_BroadcastPerformAction(attackerId, targetId, skillId);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BroadcastPerformAction(NetworkId attackerId, NetworkId targetId, string skillId)
    {
        BattleUnit attacker = Runner.FindObject(attackerId).GetComponent<BattleUnit>();
        BattleUnit target = null;
        if (targetId.IsValid) target = Runner.FindObject(targetId).GetComponent<BattleUnit>();

        RuntimeSkill skillToExec = attacker.ActiveSkills.FirstOrDefault(s => s.skillData.Id == skillId);

        if (skillToExec != null)
        {
            if (skillToExec.skillData.TargetType == SkillTargetType.AllEnemies)
            {
                StartCoroutine(ExecuteSkill_AllEnemies(attacker, skillToExec));
            }
            else
            {
                StartCoroutine(ExecuteSkill_SingleTarget(attacker, target, skillToExec));
            }
        }
    }
    IEnumerator ExecuteSkill_SingleTarget(BattleUnit attacker, BattleUnit target, RuntimeSkill skill)
    {
        localState = BattleState.Busy;

        if (attacker.animator != null)
            attacker.animator.SetTrigger("Attack");

        yield return new WaitForSeconds(skill.skillData.castDelay);

        bool isHitCompleted = false;

        if (skill.skillData.vfxPrefab != null)
        {
            Transform spawnPos = attacker.attackSpawnPoint != null ? attacker.attackSpawnPoint : attacker.transform;
            GameObject vfxObj = Instantiate(skill.skillData.vfxPrefab, spawnPos.position, Quaternion.identity);
            SkillProjectile projectile = vfxObj.GetComponent<SkillProjectile>();

            if (projectile != null)
            {
                projectile.Setup(target.transform, () =>
                {
                    PerformOnHitVisuals(target, skill);
                    ApplyDamageLogic(attacker, target, skill);
                    isHitCompleted = true;
                });
                yield return new WaitUntil(() => isHitCompleted);
            }
            else
            {
                PerformOnHitVisuals(target, skill);
                ApplyDamageLogic(attacker, target, skill);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            PerformOnHitVisuals(target, skill);
            ApplyDamageLogic(attacker, target, skill);
        }

        yield return new WaitForSeconds(1.0f);

        if (Object.HasStateAuthority)
        {
            RPC_EndTurn();
        }
    }

    IEnumerator ExecuteSkill_AllEnemies(BattleUnit attacker, RuntimeSkill skill)
    {
        localState = BattleState.Busy;
        skill.currentCooldown = skill.skillData.Cooldown;

        if (attacker.animator != null)
            attacker.animator.SetTrigger("Attack");

        yield return new WaitForSeconds(skill.skillData.castDelay);

        string attackerOwnerId = attacker.OwnerId.ToString();
        var enemies = allUnits.Where(u => u.OwnerId.ToString() != attackerOwnerId && u.CurrentHP > 0).ToList();

        int totalProjectilesFired = 0;
        int projectilesHitCount = 0;

        Transform spawnPos = attacker.attackSpawnPoint != null ? attacker.attackSpawnPoint : attacker.transform;

        foreach (var enemy in enemies)
        {
            if (skill.skillData.vfxPrefab != null)
            {
                totalProjectilesFired++;
                GameObject vfxObj = Instantiate(skill.skillData.vfxPrefab, spawnPos.position, Quaternion.identity);
                SkillProjectile projectile = vfxObj.GetComponent<SkillProjectile>();

                if (projectile != null)
                {
                    projectile.Setup(enemy.transform, () =>
                    {
                        PerformOnHitVisuals(enemy, skill);
                        ApplyDamageLogic(attacker, enemy, skill);
                        projectilesHitCount++;
                    });
                }
                else
                {
                    PerformOnHitVisuals(enemy, skill);
                    ApplyDamageLogic(attacker, enemy, skill);
                    projectilesHitCount++;
                }
            }
            else
            {
                PerformOnHitVisuals(enemy, skill);
                ApplyDamageLogic(attacker, enemy, skill);
            }
            yield return new WaitForSeconds(0.2f);
        }

        if (skill.skillData.vfxPrefab != null && totalProjectilesFired > 0)
        {
            yield return new WaitUntil(() => projectilesHitCount >= totalProjectilesFired);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1f);

        if (Object.HasStateAuthority)
        {
            RPC_EndTurn();
        }
    }
    void PerformOnHitVisuals(BattleUnit target, RuntimeSkill skill)
    {
        if (skill.skillData.isSkillHeal)
        {

        }
        else
        {
            if (target.animator != null)
            {
                target.animator.SetTrigger("Hurt");
            }
        }
    }
    void ApplyDamageLogic(BattleUnit attacker, BattleUnit target, RuntimeSkill skill)
    {
        if (Object.HasStateAuthority)
        {
            int finalValue = Mathf.RoundToInt(2f * attacker.Damage * skill.skillData.AttackMultiplier);
            bool isHeal = skill.skillData.isSkillHeal;

            if (isHeal)
            {
                Debug.Log($"Server: {attacker.unitName} heals {target.unitName} for {finalValue} HP");
            }
            else
            {
                int roll = UnityEngine.Random.Range(0, 100);
                if (roll < attacker.CritRate)
                {
                    finalValue *= 2;
                    Debug.Log($"CRITICAL HIT! Rate: {attacker.CritRate}%");
                }
                Debug.Log($"Server: {attacker.unitName} deals {finalValue} damage to {target.unitName}");
            }
            RPC_SyncDamageOrHeal(target.Object.Id, finalValue, isHeal);
        }
    }

    bool CheckBattleOver()
    {
        var owners = allUnits.Select(u => u.OwnerId.ToString()).Distinct().ToList();

        string winnerId = null;
        int teamsAlive = 0;

        foreach (var owner in owners)
        {
            if (allUnits.Any(u => u.OwnerId.ToString() == owner && u.CurrentHP > 0))
            {
                teamsAlive++;
                winnerId = owner.ToString();
            }
        }

        if (teamsAlive <= 1)
        {
            Authority_FinishMatch(winnerId);
            return true;
        }
        return false;
    }
    void MoveCurrentUnitToBack()
    {
        if (allUnits.Count == 0) return;

        BattleUnit finishedUnit = allUnits[0];
        allUnits.RemoveAt(0);

        var livingOthers = allUnits.Where(u => u.CurrentHP > 0).ToList();
        var deadOthers = allUnits.Where(u => u.CurrentHP <= 0).ToList();

        allUnits.Clear();

        if (finishedUnit.CurrentHP <= 0)
        {
            allUnits.AddRange(livingOthers);
            allUnits.AddRange(deadOthers);
            allUnits.Add(finishedUnit);
        }
        else
        {
            allUnits.AddRange(livingOthers);
            allUnits.Add(finishedUnit);
            allUnits.AddRange(deadOthers);
        }
        if (allUnits.Count > 0)
            battleHUD.UpdateTurnOrderBar(allUnits, allUnits[0]);
    }
    public void Authority_FinishMatch(string winnerWalletId)
    {
        if (Object.HasStateAuthority)
        {
            if (itemDropManager == null) return;

            var reward = itemDropManager.GetRandomLoot();
            string stringId = reward.itemData.Id;

            int amount = 1;
            int uidInt = UnityEngine.Random.Range(1000000, 999999999);
            long uid = (long)uidInt;

            string sig = "";
            if (sigService != null)
            {
                sig = sigService.GenerateWeaponSignature(winnerWalletId, reward.tokenId, amount, uid);
            }
            RPC_EndBattle(winnerWalletId, (int)reward.tokenId, stringId, sig, amount, uid.ToString());
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_EndBattle(string winnerId, int rewardTokenId, string rewardStringId, string signature, int amount, string uid)
    {
        ClearBattlefield();

        localState = (MyWalletId == winnerId) ? BattleState.Win : BattleState.Lose;

        if (localState == BattleState.Win)
        {
            Debug.Log($"VICTORY! Reward: {rewardStringId} (TokenId: {rewardTokenId}) Amount: {amount}");

            if (VictoryUI.Instance != null)
            {
                VictoryUI.Instance.SetupVictory(
                    rewardTokenId,
                    amount,      
                    uid,        
                    rewardStringId,
                    signature
                );
            }
            else
            {
                Debug.LogError("VictoryUI is NULL!");
            }
        }
        else
        {
            Debug.Log("DEFEAT!");
            if (LoseUI.Instance != null)
            {
                LoseUI.Instance.Show();
            }
        }
    }
    private void ClearBattlefield()
    {
        if (currentMarker != null)
            currentMarker.SetActive(false);

        foreach (var unit in allUnits.ToList())
        {
            if (unit != null)
            {
                if (Object.HasStateAuthority)
                {
                    if (unit.Object != null && unit.Object.IsValid)
                    {
                        Runner.Despawn(unit.Object);
                    }
                }

                if (unit.gameObject != null)
                {
                    unit.gameObject.SetActive(false);
                }
            }
        }

        allUnits.Clear();
        validTargets.Clear();

        currentSkillIndex = 0;
        currentTargetIndex = 0;
    }
    public BattleUnit GetCurrentTurnUnit()
    {
        if (allUnits != null && allUnits.Count > 0)
        {
            return allUnits[0];
        }
        return null;
    }
}