using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

// マウスの現在の状態を管理
public enum InputState
{
    Normal,         // 通常状態（ホバーのみ）
    DraggingHand,   // 手札をドラッグ中（プレイ準備）
    DraggingField,  // 場のカードをドラッグ中（攻撃準備）
    SelectingTarget // 対象を選択中
}

public class PlayerInputManager : MonoBehaviour
{
    [Header("Managers")]
    public BattleManager battleManager;
    public BattleUIManager uiManager;
    public CardLayoutManager p1HandLayout;

    [Header("UI Areas (Play)")]
    public RectTransform normalPlayArea; // 通常プレイのドロップエリア
    public RectTransform addCostPlayArea; // コスト+1プレイのドロップエリア
    public GameObject playAreaUI; // ドラッグ中のみ表示するUIの親オブジェクト

    [Header("Attack Line Settings")]
    public LineRenderer attackLine; // 攻撃時の曲線を描画する線
    public int lineResolution = 20; // 曲線の滑らかさ

    private InputState currentState = InputState.Normal;
    private GameObject draggingCard = null;
    private CardView draggingCardView = null;
    
    private Vector3 originalPos; // ドラッグ開始前の位置
    private float zDistance;

    // 選択モード用
    private int requiredTargetCount;
    private List<CardData> selectedTargets = new List<CardData>();
    private bool isPlayWithAddCost = false;

    private void Start()
    {
        if (attackLine != null) attackLine.enabled = false;
        if (playAreaUI != null) playAreaUI.SetActive(false);
    }

    private void Update()
    {
        switch (currentState)
        {
            case InputState.Normal:
                HandleNormalState();
                break;
            case InputState.DraggingHand:
                HandleDraggingHand();
                break;
            case InputState.DraggingField:
                HandleDraggingField();
                break;
            case InputState.SelectingTarget:
                HandleSelectingTarget();
                break;
        }
    }

    // ==================================================
    // 状態1: 通常時（ホバー検知とドラッグ開始）
    // ==================================================
    private void HandleNormalState()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 要件4: ホバー時の能力表示
            if (hit.collider.CompareTag("Card"))
            {
                CardView view = hit.collider.GetComponent<CardView>();
                if (view != null)
                {
                    // CardView からテキストを取得してUIに渡す想定
                    uiManager.ShowPopUp(view.AbilityText); 
                }
            }
            else
            {
                uiManager.HidePopUp();
            }

            // ドラッグ開始判定
            if (Input.GetMouseButtonDown(0) && hit.collider.CompareTag("Card"))
            {
                uiManager.HidePopUp(); // ドラッグ中はポップアップを消す
                BeginDrag(hit.collider.gameObject);
            }
        }
        else
        {
            uiManager.HidePopUp();
        }
    }

    private void BeginDrag(GameObject cardObj)
    {
        draggingCard = cardObj;
        draggingCardView = cardObj.GetComponent<CardView>();
        originalPos = cardObj.transform.position;
        zDistance = Camera.main.WorldToScreenPoint(originalPos).z;

        // 手札かフィールドかで状態を分ける (CardView がどのエリアにいるかを持っている想定)
        if (draggingCardView.IsHandCard)
        {
            currentState = InputState.DraggingHand;
            if (playAreaUI != null) playAreaUI.SetActive(true); // プレイ用UIを表示
        }
        else if (draggingCardView.IsFieldCard && draggingCardView.CurrentData.canAttackNow)
        {
            currentState = InputState.DraggingField;
            if (attackLine != null) attackLine.enabled = true;
        }
        else
        {
            // 動かせないカードはすぐにリセット
            draggingCard = null;
            draggingCardView = null;
        }
    }

    // ==================================================
    // 状態2: 手札をドラッグ中（プレイ）
    // ==================================================
    private void HandleDraggingHand()
    {
        UpdateCardPositionToMouse();

        if (Input.GetMouseButtonUp(0))
        {
            if (playAreaUI != null) playAreaUI.SetActive(false);

            // UIとマウスが重なっているか判定
            if (RectTransformUtility.RectangleContainsScreenPoint(addCostPlayArea, Input.mousePosition))
            {
                AttemptPlay(true); // コスト+1 でプレイ
            }
            else if (RectTransformUtility.RectangleContainsScreenPoint(normalPlayArea, Input.mousePosition))
            {
                AttemptPlay(false); // 通常プレイ
            }
            else
            {
                CancelDrag(); // UIに重ならなかったら元の位置へ戻る
            }
        }
    }

    // ==================================================
    // 状態3: 場のカードをドラッグ中（攻撃）
    // ==================================================
    private void HandleDraggingField()
    {
        // カード自体は動かさず、曲線の矢印だけを描画する
        DrawAttackCurve(draggingCard.transform.position, Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
        {
            if (attackLine != null) attackLine.enabled = false;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Card"))
                {
                    CardView targetView = hit.collider.GetComponent<CardView>();
                    // 敵のフィールドのカードなら攻撃
                    if (targetView != null && !targetView.IsMyCard)
                    {
                        Debug.Log("相手カードへ攻撃！");
                        // battleManager.SubmitAttack(draggingCardView.CurrentData, targetView.CurrentData);
                        // ↑ バトルマネージャーへ送信。その後、マネージャーがCardViewの戦闘エフェクトを呼び出す
                    }
                }
                else if (hit.collider.CompareTag("EnemyPlayer"))
                {
                    Debug.Log("相手プレイヤーへダイレクトアタック！");
                    // battleManager.SubmitDirectAttack(draggingCardView.CurrentData);
                }
            }

            CancelDrag(); // ドラッグ状態を解除（カードは動かしていないので元のままでOK）
        }
    }

    // ==================================================
    // 状態4: 対象選択中
    // ==================================================
    private void HandleSelectingTarget()
    {
        // 選択対象外の場所をクリックしたらキャンセルして盤面を戻す
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag("Card"))
            {
                CardView targetView = hit.collider.GetComponent<CardView>();
                if (targetView != null)
                {
                    // 点滅エフェクトのオンオフ
                    if (selectedTargets.Contains(targetView.CurrentData))
                    {
                        selectedTargets.Remove(targetView.CurrentData);
                        targetView.SetHighlight(false);
                    }
                    else
                    {
                        selectedTargets.Add(targetView.CurrentData);
                        targetView.SetHighlight(true);

                        // 規定枚数に達したらプレイ決定
                        if (selectedTargets.Count >= requiredTargetCount)
                        {
                            ConfirmPlayWithTargets();
                        }
                    }
                }
            }
            else
            {
                // 背景など関係ないところをクリックしたらキャンセル
                CancelTargetSelection();
            }
        }
    }

    // ==================================================
    // 補助機能
    // ==================================================
    private void UpdateCardPositionToMouse()
    {
        Vector3 mouseScreenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance);
        draggingCard.transform.position = Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }

    private void CancelDrag()
    {
        if (draggingCard != null)
        {
            // DOTween でスッと元の位置に戻る演出
            draggingCard.transform.DOMove(originalPos, 0.25f).SetEase(Ease.OutCubic);
        }
        draggingCard = null;
        draggingCardView = null;
        currentState = InputState.Normal;
    }

    // プレイを実行（対象選択が必要か問い合わせる）
    private void AttemptPlay(bool isAddCost)
    {
        isPlayWithAddCost = isAddCost;
        
        // BattleManagerに「このカードは対象選択が必要か？何枚か？」を問い合わせる想定
        int targetCount = battleManager.RequiresTargetCount(draggingCardView.CurrentData);

        if (targetCount > 0)
        {
            // 対象選択モードへ移行
            requiredTargetCount = targetCount;
            selectedTargets.Clear();
            currentState = InputState.SelectingTarget;
            
            // LayoutManager に指示して、対象可能なカードを中央に並べてもらう
            // p1HandLayout.BeginSelectionMode(validTargetIds); 
            
            // ※ドラッグしていた手札のカードは一旦非表示にするか、元の位置に戻しておく
            draggingCard.SetActive(false); 
        }
        else
        {
            // そのままプレイ
            Debug.Log($"カードをプレイ！ (コスト追加: {isAddCost})");
            // battleManager.SubmitPlay(draggingCardView.CurrentData, isPlayWithAddCost, null);
            
            draggingCard = null;
            draggingCardView = null;
            currentState = InputState.Normal;
        }
    }

    private void ConfirmPlayWithTargets()
    {
        Debug.Log("対象を選んでカードをプレイ！");
        // battleManager.SubmitPlay(draggingCardView.CurrentData, isPlayWithAddCost, selectedTargets);

        // 選択演出の解除
        foreach (var data in selectedTargets)
        {
            // FindCardObjectして SetHighlight(false) する処理
        }

        // LayoutManager に並びを元に戻してもらう
        // p1HandLayout.EndSelectionMode();

        draggingCard.SetActive(true); // 隠していたカードを戻す（プレイされて移動する）
        draggingCard = null;
        draggingCardView = null;
        currentState = InputState.Normal;
    }

    private void CancelTargetSelection()
    {
        Debug.Log("カードのプレイをキャンセルしました。");
        // p1HandLayout.EndSelectionMode(); // 盤面を元に戻す

        foreach (var data in selectedTargets) { /* ハイライト解除 */ }
        
        draggingCard.SetActive(true);
        CancelDrag();
    }

    // ベジェ曲線を描画して攻撃の矢印を表現
    private void DrawAttackCurve(Vector3 startWorldPos, Vector3 mouseScreenPos)
    {
        mouseScreenPos.z = zDistance;
        Vector3 endWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        
        // 制御点（中間地点から少し上方向に膨らませる）
        Vector3 controlPos = (startWorldPos + endWorldPos) / 2f;
        controlPos.y += 2.0f; // 膨らみ具合

        attackLine.positionCount = lineResolution + 1;
        for (int i = 0; i <= lineResolution; i++)
        {
            float t = i / (float)lineResolution;
            Vector3 point = CalculateBezierPoint(t, startWorldPos, controlPos, endWorldPos);
            attackLine.SetPosition(i, point);
        }
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0; // (1-t)^2 * P0
        p += 2 * u * t * p1; // 2(1-t)t * P1
        p += tt * p2;        // t^2 * P2
        return p;
    }
}