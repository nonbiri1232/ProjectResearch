using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public struct CardData : INetworkSerializable
{
    public int uniqueId;
    public int id;
    public int type; //1.Object 2.Method 3.Scope
    public int cost;
    public int atk;
    public int hp;
    public bool canAttackNow;
    public bool isProxy;

    // 通信で送るためのパッキング処理
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref uniqueId);
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref cost);
        serializer.SerializeValue(ref atk);
        serializer.SerializeValue(ref hp);
        serializer.SerializeValue(ref canAttackNow);
        serializer.SerializeValue(ref isProxy);
    }
}

// One recipient-specific snapshot. Opponent hand/deck identities never cross the wire.
public struct LocalBattleSnapshot : INetworkSerializable
{
    public CardData[] hand, field, enemyField, garbage, enemyGarbage;
    public int[] selfStats, enemyStats, attackTargets, directAttackers;
    public int revision, turnNumber, result;
    public PhaseState phase;
    public bool myTurn, needsMarigan, finished, reset;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref hand);
        serializer.SerializeValue(ref field);
        serializer.SerializeValue(ref enemyField);
        serializer.SerializeValue(ref garbage);
        serializer.SerializeValue(ref enemyGarbage);
        serializer.SerializeValue(ref selfStats);
        serializer.SerializeValue(ref enemyStats);
        serializer.SerializeValue(ref attackTargets);
        serializer.SerializeValue(ref directAttackers);
        serializer.SerializeValue(ref revision);
        serializer.SerializeValue(ref turnNumber);
        serializer.SerializeValue(ref result);
        serializer.SerializeValue(ref phase);
        serializer.SerializeValue(ref myTurn);
        serializer.SerializeValue(ref needsMarigan);
        serializer.SerializeValue(ref finished);
        serializer.SerializeValue(ref reset);
    }
}

public class LocalBattleManager : BattleManager
{
    [Header("Shared battle view")]
    [SerializeField] private BattleUIManager uiManager;
    [SerializeField] private PlayerInputManager inputManager;
    [SerializeField] private CardConect cardDatabase;
    [SerializeField] private AIDeckCatalog fallbackDeckCatalog;
    [SerializeField] private CardLayoutManager p1HandLayout, p1FieldLayout, p1GarbageLayout, p1DeckLayout, p1MariganLayout;
    [SerializeField] private CardLayoutManager p2HandLayout, p2FieldLayout, p2GarbageLayout, p2DeckLayout;
    [SerializeField] private Transform enemyAttackTarget;

    private readonly Dictionary<ulong, int[]> decks = new Dictionary<ulong, int[]>();
    private readonly HashSet<ulong> awaitingPresentation = new HashSet<ulong>();
    private readonly HashSet<ulong> rematchRequests = new HashSet<ulong>();
    private Player host, client, winner;
    private ulong peerId = ulong.MaxValue;
    private int revision;
    private bool serverBusy, hasBoard, requestPending, disconnected, selectingMarigan, selectingGarbage;
    private LocalBattleSnapshot board;
    private UnityEngine.UI.Button garbageButton, rematchButton, surrenderButton;
    private CardLayoutManager[] Layouts => new[] { p1DeckLayout, p1HandLayout, p1FieldLayout,
        p1GarbageLayout, p1MariganLayout, p2DeckLayout, p2HandLayout, p2FieldLayout, p2GarbageLayout };

    public bool IsMyTurn => hasBoard && board.myTurn;
    public override PhaseState CurrentPhase => hasBoard ? board.phase : PhaseState.Start;
    public override bool IsFinished => disconnected || (hasBoard && board.finished);
    public override bool CanAct() => IsSpawned && hasBoard && !IsFinished && !isPresenting &&
        !requestPending && board.myTurn && !board.needsMarigan;

    private void Awake()
    {
        if (inputManager == null) inputManager = FindAnyObjectByType<PlayerInputManager>();
        if (uiManager == null && inputManager != null) uiManager = inputManager.uiManager;
        if (inputManager != null) inputManager.battleManager = this;
        if (uiManager == null) return;
        uiManager.battleManager = this;
        uiManager.inputManager = inputManager;
        uiManager.HideMarigan();
        uiManager.endGame.SetActive(false);
        uiManager.endTurnButton.interactable = false;
        uiManager.systemText.text = "対戦相手との同期を待っています";
        if (cardDatabase == null) cardDatabase = uiManager.cardDatabase;
        garbageButton = uiManager.CreateActionButton("選択してガベージ確定", uiManager.endTurnButton.transform.parent,
            new Vector2(0.84f, 0.58f), () => inputManager.ConfirmSelfGarbage());
        garbageButton.gameObject.SetActive(false);
        rematchButton = uiManager.CreateActionButton("再戦を希望", uiManager.endGame.transform,
            new Vector2(0.5f, 0.25f), RequestRematch);
        surrenderButton = uiManager.CreateActionButton("投了", uiManager.endTurnButton.transform.parent,
            new Vector2(0.93f, 0.95f), Surrender);
        surrenderButton.interactable = false;
        uiManager.CreateActionButton("タイトルへ", uiManager.endTurnButton.transform.parent,
            new Vector2(0.07f, 0.95f), ReturnToTitle);
        // The inherited scene's lobby button must close this connection first.
        foreach (var button in uiManager.endGame.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            if (button == rematchButton) continue;
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.onClick.AddListener(ReturnToTitle);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        disconnected = false;
        NetworkManager.OnClientDisconnectCallback += OnDisconnected;
        DeckManager.LoadDeck();
        List<int> saved = DeckManager.player1Deck;
        if (saved == null || saved.Count < 5)
        {
            if (fallbackDeckCatalog == null || !fallbackDeckCatalog.TryCreateRandomDeck(new List<int> { 0 },
                out List<Card> fallback, out int ignored))
            {
                if (uiManager != null) uiManager.systemText.text = "デッキを保存してから対戦を開始してください";
                return;
            }
            saved = fallback.Select(Card.GetCardId).ToList();
        }
        SubmitDeckRpc(saved.ToArray());
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnDisconnected;
        base.OnNetworkDespawn();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitDeckRpc(int[] ids, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (gm != null || !NetworkManager.ConnectedClientsIds.Contains(sender)) return;
        if (sender != Unity.Netcode.NetworkManager.ServerClientId)
        {
            if (peerId != ulong.MaxValue && peerId != sender) return;
            peerId = sender;
        }
        if (ids == null || ids.Length < 5 || ids.Length > 100 || Player.ChangeCard(ids).Count != ids.Length)
        {
            Reject(sender, "対戦デッキが不正です。デッキを保存し直してください");
            return;
        }
        decks[sender] = ids;
        if (decks.Count == 2) StartMatch();
    }

    private void StartMatch()
    {
        if (gm != null) gm.OnGameFinished -= OnGameFinished;
        host = new Player(Player.ChangeCard(decks[Unity.Netcode.NetworkManager.ServerClientId]));
        client = new Player(Player.ChangeCard(decks[peerId]));
        localPlayer = host;
        remotePlayer = client;
        winner = null;
        rematchRequests.Clear();
        gm = Random.Range(0, 2) == 0 ? new GameManager(host, client) : new GameManager(client, host);
        gm.OnGameFinished += OnGameFinished;
        PublishState(-1, 0, default, default, false, true);
    }

    private void OnGameFinished(Player value) { winner = value; }
    private Player PlayerFor(ulong id) => id == Unity.Netcode.NetworkManager.ServerClientId ? host : id == peerId ? client : null;
    private Player Enemy(Player p) => p == host ? client : host;

    public override int RequiresTargetCount(CardData data)
    {
        Card card = Card.CreateCardInstance(data);
        return card?.select != null && card.select.isSelectConstructor ? card.select.numOfSelect : 0;
    }

    public override bool TryGetPlayTargets(CardData data, out where area, out List<int> ids)
    {
        ids = new List<int>();
        area = where.None;
        Card card = Card.CreateCardInstance(data);
        if (!hasBoard || card?.select == null || !card.select.isSelectConstructor) return false;
        area = card.select.whereTarget;
        CardData[] candidates = area == where.hand ? board.hand : area == where.selfField ? board.field :
            area == where.enemyField ? board.enemyField : new CardData[0];
        ids = candidates.Where(c => c.uniqueId != data.uniqueId).Select(c => c.uniqueId).ToList();
        return ids.Count >= card.select.numOfSelect;
    }

    public override bool CanAttackTarget(CardData source, CardData? target = null)
    {
        if (!CanAct() || CurrentPhase != PhaseState.Main ||
            !board.field.Any(c => c.uniqueId == source.uniqueId && c.canAttackNow)) return false;
        return target.HasValue ? board.attackTargets.Contains(target.Value.uniqueId) : board.directAttackers.Contains(source.uniqueId);
    }

    private void SendAction(ActionType type, int sourceId, bool addCost, int[] targetIds)
    {
        bool mulligan = type == ActionType.Marigan;
        if (!IsSpawned || !hasBoard || IsFinished || isPresenting || requestPending ||
            (mulligan ? !board.needsMarigan : !CanAct())) return;
        requestPending = true;
        UpdateControls();
        SubmitActionRpc(type, sourceId, addCost, targetIds ?? new int[0]);
    }

    public override void SubmitPlay(CardData source, bool addCost, List<CardData> targets = null)
        => SendAction(ActionType.Play, source.uniqueId, addCost, targets?.Select(c => c.uniqueId).ToArray());
    public override void SubmitAttack(CardData source, CardData? target = null)
    {
        if (!CanAttackTarget(source, target)) return;
        SendAction(ActionType.Attack, source.uniqueId, false, target.HasValue ? new[] { target.Value.uniqueId } : null);
    }
    public override void SubmitMarigan(List<CardData> selected)
        => SendAction(ActionType.Marigan, -1, false, selected?.Select(c => c.uniqueId).ToArray());
    public override void SubmitSelfGarbage(List<CardData> selected)
        => SendAction(ActionType.SelfGarbage, -1, false, selected?.Select(c => c.uniqueId).ToArray());
    public override void SubmitEndTurn() => SendAction(ActionType.End, -1, false, null);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitActionRpc(ActionType type, int sourceId, bool addCost, int[] targetIds, RpcParams rpcParams = default)
    {
        ulong id = rpcParams.Receive.SenderClientId;
        Player actor = PlayerFor(id);
        if (actor == null || gm == null || gm.currentState == GameState.Finished) { Reject(id, "対戦は終了しています"); return; }
        if (serverBusy) { Reject(id, "演出の完了を待ってから操作してください"); return; }
        bool mulligan = type == ActionType.Marigan;
        if (mulligan ? !gm.NeedsMarigan(actor) : gm.turn != actor) { Reject(id, "現在は操作できません"); return; }
        Player enemy = Enemy(actor);
        Card source = type == ActionType.Play ? actor.hand.FirstOrDefault(c => c.uniqueId == sourceId) :
            type == ActionType.Attack ? actor.field.FirstOrDefault(c => c.uniqueId == sourceId) : null;
        List<Card> pool = type == ActionType.Marigan ? actor.hand : type == ActionType.SelfGarbage ? actor.field :
            type == ActionType.Attack ? enemy.field : source != null ? LegalActionGenerator.GetPlayTargetPool(actor, enemy, source) : new List<Card>();
        var targets = new List<Card>();
        foreach (int targetId in targetIds ?? new int[0])
        {
            Card target = pool.FirstOrDefault(c => c.uniqueId == targetId);
            if (target == null || targets.Contains(target)) { Reject(id, "選択した対象が無効です"); return; }
            targets.Add(target);
        }
        if ((type == ActionType.Play || type == ActionType.Attack) && source == null) { Reject(id, "カードが見つかりません"); return; }
        if (type == ActionType.Attack && targets.Count > 1) { Reject(id, "攻撃対象は1枚までです"); return; }
        if (type == ActionType.SelfGarbage && (gm.currentPhase != PhaseState.Start || gm.systemTurn == 1)) { Reject(id, "セルフガベージのフェーズではありません"); return; }
        CardData sourceBefore = source != null ? Card.PackingCard(source) : default;
        CardData targetBefore = type == ActionType.Attack && targets.Count > 0 ? Card.PackingCard(targets[0]) : default;
        var action = new PlayerAction(type) { sourceCard = source, targetCard = targets, isAddCost = addCost };
        serverBusy = true;
        if (!gm.ExecuteAction(actor, enemy, action))
        {
            serverBusy = false;
            Reject(id, "コストや対象などの条件を満たしていないため、実行できません");
            return;
        }
        int presentation = type == ActionType.Play ? 1 : type == ActionType.Attack ? 2 : 0;
        PublishState(presentation, id, type == ActionType.Play ? Card.PackingCard(source) : sourceBefore,
            targetBefore, type == ActionType.Attack && targets.Count > 0, false);
    }

    private void Reject(ulong id, string message)
    {
        if (!NetworkManager.ConnectedClientsIds.Contains(id)) return;
        RejectedRpc(message, RpcTarget.Single(id, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void RejectedRpc(string message, RpcParams rpcParams = default)
    {
        requestPending = false;
        selectingMarigan = selectingGarbage = false;
        if (!isPresenting && hasBoard)
        {
            inputManager.ResetSelection();
            selectingMarigan = selectingGarbage = false;
            SyncView();
            p1HandLayout.RefreshCard();
        }
        if (uiManager != null) uiManager.systemText.text = message;
    }

    private void PublishState(int presentation, ulong actorId, CardData source, CardData target, bool hasTarget, bool reset)
    {
        serverBusy = true;
        revision++;
        awaitingPresentation.Clear();
        awaitingPresentation.Add(Unity.Netcode.NetworkManager.ServerClientId);
        awaitingPresentation.Add(peerId);
        ReceiveStateRpc(Snapshot(host, reset), presentation, actorId, source, target, hasTarget,
            RpcTarget.Single(Unity.Netcode.NetworkManager.ServerClientId, RpcTargetUse.Temp));
        ReceiveStateRpc(Snapshot(client, reset), presentation, actorId, source, target, hasTarget,
            RpcTarget.Single(peerId, RpcTargetUse.Temp));
    }

    private LocalBattleSnapshot Snapshot(Player self, bool reset)
    {
        Player enemy = Enemy(self);
        var field = new List<Card>(self.field);
        var enemyField = new List<Card>(enemy.field);
        if (gm.currentScope != null)
        {
            var scopeField = gm.currentScope.player == self ? field : enemyField;
            if (!scopeField.Contains(gm.currentScope)) scopeField.Add(gm.currentScope);
        }
        return new LocalBattleSnapshot
        {
            hand = Pack(self.hand), field = Pack(field), enemyField = Pack(enemyField),
            garbage = Pack(self.garbage), enemyGarbage = Pack(enemy.garbage),
            selfStats = Stats(self), enemyStats = Stats(enemy),
            attackTargets = LegalActionGenerator.GetValidAttackTargets(enemy).Select(c => c.uniqueId).ToArray(),
            directAttackers = self.field.Where(c => LegalActionGenerator.CanDirectAttack(c, enemy)).Select(c => c.uniqueId).ToArray(),
            revision = revision, turnNumber = gm.systemTurn, phase = gm.currentPhase,
            myTurn = gm.turn == self, needsMarigan = gm.NeedsMarigan(self),
            finished = gm.currentState == GameState.Finished,
            result = winner == null ? 0 : winner == self ? 1 : -1, reset = reset
        };
    }
    private static int[] Stats(Player p) => new[] { p.hand.Count, p.garbage.Count, p.maxMemory, p.fieldCost, p.usableMemory, p.usedMemory, p.deck.Count };
    private CardData[] Pack(List<Card> cards) => cards.Select(c =>
    {
        CardData data = Card.PackingCard(c);
        data.canAttackNow = c.player == gm.turn && gm.currentPhase == PhaseState.Main &&
            c.player.field.Contains(c) && LegalActionGenerator.IsPotentialAttacker(c);
        return data;
    }).ToArray();

    [Rpc(SendTo.SpecifiedInParams)]
    private void ReceiveStateRpc(LocalBattleSnapshot state, int presentation, ulong actorId,
        CardData source, CardData target, bool hasTarget, RpcParams rpcParams = default)
    {
        if (hasBoard && state.revision <= board.revision) return;
        board = state;
        hasBoard = true;
        requestPending = false;
        isPresenting = true;
        if (state.reset)
        {
            StopAllCoroutines();
            inputManager.ResetSelection();
            selectingMarigan = selectingGarbage = false;
            foreach (var layout in Layouts) { layout.ClearCards(); layout.Initialize(); }
            uiManager.endGame.SetActive(false);
            rematchButton.interactable = true;
            rematchButton.GetComponentInChildren<TMPro.TMP_Text>().text = "再戦を希望";
        }
        UpdateControls();
        StartCoroutine(PresentState(presentation, actorId == NetworkManager.LocalClientId, source, target, hasTarget, state.revision));
    }

    private System.Collections.IEnumerator PresentState(int presentation, bool mine, CardData source, CardData target, bool hasTarget, int stateRevision)
    {
        CardLayoutManager from = mine ? p1HandLayout : p2HandLayout;
        CardLayoutManager field = mine ? p1FieldLayout : p2FieldLayout;
        CardLayoutManager targetField = mine ? p2FieldLayout : p1FieldLayout;
        if (presentation == 1)
        {
            // The remote hand contains anonymous backs. Reuse one without revealing the others.
            if (!mine)
            {
                GameObject back = from.FindCardObject(Back(-3000, board.enemyStats[0]));
                if (back != null) field.ReceiveCard(from, source, back);
            }
            field.PresentPlay(from, source, mine, Setting(source)?.ability, Setting(source)?.cardImage);
            yield return new WaitForSeconds(0.55f);
        }
        else if (presentation == 2)
        {
            var obj = hasTarget ? targetField.FindCardObject(target) : null;
            Vector3 position = obj != null ? obj.transform.position :
                mine && enemyAttackTarget != null ? enemyAttackTarget.position : targetField.CenterPosition;
            bool done = false;
            field.PlayAttack(source, position, () => done = true);
            yield return new WaitUntil(() => done);
        }
        if (board.reset) foreach (var layout in Layouts) layout.BeginBatchUpdate();
        SyncView();
        if (board.reset) foreach (var layout in Layouts) layout.EndBatchUpdate();
        yield return new WaitForSeconds(0.55f);
        if (disconnected || !IsSpawned) yield break;
        PresentationDoneRpc(stateRevision);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void PresentationDoneRpc(int stateRevision, RpcParams rpcParams = default)
    {
        if (stateRevision != revision) return;
        awaitingPresentation.Remove(rpcParams.Receive.SenderClientId);
        if (awaitingPresentation.Count != 0) return;
        serverBusy = false;
        UnlockRpc(revision);
    }
    [Rpc(SendTo.Everyone)]
    private void UnlockRpc(int stateRevision)
    {
        if (!hasBoard || board.revision != stateRevision || disconnected) return;
        isPresenting = false;
        UpdateControls();
        if (board.finished) ShowResult(board.result, null);
    }

    private static CardData Back(int zone, int index) => new CardData { uniqueId = zone - index, id = -1 };
    private static CardData[] Backs(int count, int zone) => Enumerable.Range(0, count).Select(i => Back(zone, i)).ToArray();
    private CardSetting Setting(CardData data) => data.id < 0 || cardDatabase == null ? null :
        cardDatabase.cards.FirstOrDefault(s => s.className == Card.GetCardClassName(data.id));

    private void SyncView()
    {
        var hand = board.needsMarigan ? p1MariganLayout : p1HandLayout;
        var destinations = new[] { p1DeckLayout, hand, p1FieldLayout, p1GarbageLayout,
            p2DeckLayout, p2HandLayout, p2FieldLayout, p2GarbageLayout };
        var zones = new[] { Backs(board.selfStats[6], -1000), board.hand, board.field, board.garbage,
            Backs(board.enemyStats[6], -2000), Backs(board.enemyStats[0], -3000), board.enemyField, board.enemyGarbage };
        var live = new HashSet<int>(zones.SelectMany(z => z).Select(c => c.uniqueId));
        foreach (var layout in Layouts) layout.RetainCards(live);
        if (board.needsMarigan) uiManager.ShowMarigan(); else uiManager.HideMarigan();
        for (int i = 0; i < zones.Length; i++)
        {
            CardLayoutManager destination = destinations[i];
            foreach (CardData data in zones[i])
            {
                if (destination.FindCardObject(data) == null)
                {
                    var source = Layouts.FirstOrDefault(l => l.FindCardObject(data) != null);
                    if (source != null) destination.ReceiveCard(source, data, source.FindCardObject(data));
                    else destination.CreateCard(data);
                }
                var setting = destination.IsFaceDown ? null : Setting(data);
                destination.UpdateCard(data, i < 4, setting?.ability, !destination.IsFaceDown, setting?.cardImage);
            }
        }
        uiManager.UpdateNetworkUI(board.myTurn, board.phase, board.selfStats, board.enemyStats);
        UpdateControls();
    }

    private void UpdateControls()
    {
        if (uiManager == null || !hasBoard) return;
        bool garbage = !IsFinished && !board.needsMarigan && board.myTurn && board.phase == PhaseState.Start && board.turnNumber > 1;
        if (board.needsMarigan && !selectingMarigan) inputManager.StartMariganSelection();
        else if (garbage && !selectingGarbage) inputManager.StartSelfGarbageSelection();
        else if ((!board.needsMarigan && selectingMarigan) || (!garbage && selectingGarbage)) inputManager.ResetSelection();
        selectingMarigan = board.needsMarigan;
        selectingGarbage = garbage;
        uiManager.mariganConfirmButton.interactable = !isPresenting && !requestPending && board.needsMarigan && !IsFinished;
        garbageButton.gameObject.SetActive(garbage);
        garbageButton.interactable = CanAct();
        uiManager.endTurnButton.interactable = CanAct() && board.phase == PhaseState.Main;
        surrenderButton.interactable = IsSpawned && !IsFinished && !isPresenting && !requestPending;
        if (hasBoard && board.needsMarigan && isPresenting) uiManager.systemText.text = "相手の画面の準備を待っています";
    }

    private void ShowResult(int result, string reason)
    {
        inputManager.ResetSelection();
        uiManager.HideMarigan();
        uiManager.HidePopUp();
        garbageButton.gameObject.SetActive(false);
        uiManager.endGame.SetActive(true);
        uiManager.endGame.transform.SetAsLastSibling();
        uiManager.endText.text = (result > 0 ? "勝利" : result < 0 ? "敗北" : "引き分け") +
            (string.IsNullOrEmpty(reason) ? "" : "\n" + reason);
        rematchButton.interactable = !disconnected && IsSpawned;
    }

    public void Surrender()
    {
        if (IsSpawned && hasBoard && !IsFinished && !isPresenting) SurrenderRpc();
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SurrenderRpc(RpcParams rpcParams = default)
    {
        Player actor = PlayerFor(rpcParams.Receive.SenderClientId);
        if (actor == null || gm == null || serverBusy || gm.currentState == GameState.Finished) return;
        gm.Surrender(actor);
        PublishState(0, rpcParams.Receive.SenderClientId, default, default, false, false);
    }
    public void RequestRematch()
    {
        if (IsSpawned && IsFinished && !disconnected && !isPresenting) RequestRematchRpc();
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestRematchRpc(RpcParams rpcParams = default)
    {
        ulong id = rpcParams.Receive.SenderClientId;
        if (PlayerFor(id) == null || gm == null || gm.currentState != GameState.Finished || serverBusy) return;
        rematchRequests.Add(id);
        RematchStatusRpc(rematchRequests.Contains(Unity.Netcode.NetworkManager.ServerClientId), rematchRequests.Contains(peerId));
        if (rematchRequests.Count == 2) StartMatch();
    }
    [Rpc(SendTo.Everyone)]
    private void RematchStatusRpc(bool hostReady, bool clientReady)
    {
        bool mine = IsServer ? hostReady : clientReady;
        rematchButton.interactable = !mine;
        rematchButton.GetComponentInChildren<TMPro.TMP_Text>().text = mine ? "相手の再戦希望待ち" : "相手が再戦を希望";
    }

    private void OnDisconnected(ulong id)
    {
        if (IsServer && id != peerId) return;
        disconnected = true;
        requestPending = false;
        isPresenting = false;
        serverBusy = false;
        StopAllCoroutines();
        UpdateControls();
        ShowResult(hasBoard && board.finished ? board.result : 1, "対戦相手との接続が切れました");
    }
    public void ReturnToTitle()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= OnDisconnected;
            NetworkManager.Shutdown();
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
    }

    // Compatibility adapters for the old button UI; all commands still pass
    // through the same authenticated, uniqueId-based server endpoint.
    public void DecideMariganRpc(int[] typeIds)
    {
        if (!hasBoard) return;
        var available = board.hand.ToList();
        var selected = new List<CardData>();
        foreach (int id in typeIds)
        {
            int index = available.FindIndex(c => c.id == id);
            if (index < 0) return;
            selected.Add(available[index]); available.RemoveAt(index);
        }
        SubmitMarigan(selected);
    }
    public void GarbageActionRpc(int[] indices)
    {
        if (!hasBoard || indices.Any(i => i < 0 || i >= board.field.Length)) return;
        SubmitSelfGarbage(indices.Select(i => board.field[i]).ToList());
    }
    public void TurnEndRpc() => SubmitEndTurn();
    public void PlayActionRpc(bool addCost, int index)
    {
        if (hasBoard && index >= 0 && index < board.hand.Length) SubmitPlay(board.hand[index], addCost);
    }
    public void PlayActionSelectRpc(bool addCost, int index, int[] indices, where area)
    {
        if (!hasBoard || index < 0 || index >= board.hand.Length) return;
        var pool = area == where.hand ? board.hand : area == where.selfField ? board.field : board.enemyField;
        if (indices.Any(i => i < 0 || i >= pool.Length)) return;
        SubmitPlay(board.hand[index], addCost, indices.Select(i => pool[i]).ToList());
    }
    public void AskCanAttackRpc(int index)
    {
        if (hasBoard && CanAct() && index >= 0 && index < board.field.Length && board.field[index].canAttackNow)
            FindAnyObjectByType<LocalBattleVisual>()?.OpenAttackSelectUI(index);
    }
    public void AttackActionRpc(int sourceIndex, int targetIndex)
    {
        if (!hasBoard || sourceIndex < 0 || sourceIndex >= board.field.Length || targetIndex >= board.enemyField.Length) return;
        SubmitAttack(board.field[sourceIndex], targetIndex < 0 ? (CardData?)null : board.enemyField[targetIndex]);
    }
}
