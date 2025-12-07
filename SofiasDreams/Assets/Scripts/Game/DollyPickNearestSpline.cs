using System.Collections;  
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class DollyPickNearestSpline : MonoBehaviour
{
    [Header("Sources")]
    public List<SplineContainer> candidates;    // по 1 сплайну в каждом контейнере
    public Transform target;                    // игрок
    public Rigidbody2D targetRb;                // опционально: для точной скорости

    [Header("Switch logic")]
    public float reevaluateEvery = 0.15f;       // переоценка, сек
    public float hysteresis = 0.7f;             // «запас» чтобы не щёлкало
    public float switchCooldown = 0.5f;         // пауза между свитчами

    [Header("Smooth handoff between splines")]
    public float minBridgeDistance = 1.5f;      // если ближе — защёлкиваемся сразу
    public float bridgeSpeed = 8f;              // юнит/сек (ручной «мост»)
    public AnimationCurve bridgeEase = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Param handoff after switch")]
    public float anticipateTime = 0.35f;        // предсказание позиции цели вперёд (сек)
    public float minSpeedToAnticipate = 1.5f;   // порог скорости для предсказания
    public float paramHandoffTime = 0.30f;          // длительность сглаживания t
    public float paramHandoffResponsiveness = 12f;  // скорость догонки t

    CinemachineSplineDolly dolly;
    SplineContainer current;
    float nextEval, nextAllowedSwitch;
    bool bridging;
    Vector2 lastTargetPos;

    void Awake()
    {
        dolly = GetComponent<CinemachineSplineDolly>();
        if (target) lastTargetPos = target.position;
    }

    void OnEnable() { EvaluateNow(); }

    void LateUpdate()
    {
        if (Time.time >= nextEval && !bridging)
            EvaluateNow();
    }

    void EvaluateNow()
    {
        nextEval = Time.time + reevaluateEvery;
        if (!target || candidates == null || candidates.Count == 0) return;

        // предсказанная позиция цели (для выбора сплайна)
        Vector3 predicted = GetPredictedTargetPos();

        // лучший сплайн относительно predicted
        SplineContainer best = FindBest(predicted);

        if (best && best != current)
        {
            float cur = current ? Mathf.Sqrt(DistanceSqrWorldXY(predicted, current)) : float.PositiveInfinity;
            float nw  = Mathf.Sqrt(DistanceSqrWorldXY(predicted, best));
            if (nw + hysteresis < cur && Time.time >= nextAllowedSwitch)
                StartCoroutine(BridgeTo(best, predicted));
        }
    }

    // поиск контейнера с минимальной дистанцией до worldPos (только XY)
    SplineContainer FindBest(Vector3 worldPos)
    {
        SplineContainer best = current;
        float bestSqr = float.PositiveInfinity;
        foreach (var sc in candidates)
        {
            if (!sc || sc.Splines.Count == 0) continue;
            float d2 = DistanceSqrWorldXY(worldPos, sc);
            if (d2 < bestSqr) { bestSqr = d2; best = sc; }
        }
        return best;
    }

    // плавный «мост» к ближайшей точке нового сплайна
    IEnumerator BridgeTo(SplineContainer to, Vector3 referencePos)
    {
        bridging = true;

        // ближайшая точка и параметр t на новом сплайне (в локале контейнера)
        var s = to.Splines[0];
        float3 localRef = (float3)to.transform.InverseTransformPoint(referencePos);
        SplineUtility.GetNearestPoint(s, localRef, out float3 nearestLocal, out float tNorm);
        Vector3 end = to.transform.TransformPoint((Vector3)nearestLocal);

        Vector3 start = transform.position;
        end.z = start.z; // 2D

        float distXY = Vector2.Distance((Vector2)start, (Vector2)end);

        if (distXY <= minBridgeDistance)
        {
            // близко: защёлкиваемся сразу и мягко догоняем параметр t
            LockToSplineInstant(to, tNorm);
            yield return StartCoroutine(ParamHandoff(to));
            nextAllowedSwitch = Time.time + switchCooldown;
            bridging = false;
            yield break;
        }

        // временно отключаем влияние CM и движемся вручную
        var vcam = GetComponent<CinemachineCamera>();
        Transform savedFollow = vcam.Follow;
        Transform savedLookAt = vcam.LookAt;

        dolly.enabled = false;
        vcam.Follow = null;
        vcam.LookAt = null;

        float duration = Mathf.Max(distXY / Mathf.Max(bridgeSpeed, 0.01f), 0.05f);

        float t = 0f;
        while (t < 1f)
        {
            yield return new WaitForEndOfFrame();              // после всех апдейтов CM
            t += Time.unscaledDeltaTime / duration;
            float e = bridgeEase.Evaluate(Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(start, end, e);
        }

        // фиксируемся на новом пути и мягко догоняем параметр t
        LockToSplineInstant(to, tNorm);
        yield return StartCoroutine(ParamHandoff(to));

        // возвращаем Follow/LookAt
        yield return null; // один кадр — чтобы всё устаканилось
        vcam.Follow = savedFollow;
        vcam.LookAt = savedLookAt;

        nextAllowedSwitch = Time.time + switchCooldown;
        bridging = false;
    }

    void LockToSplineInstant(SplineContainer to, float tNorm)
    {
        current = to;

        // назначаем путь и позицию на нём
        dolly.Spline = to;
        dolly.PositionUnits = PathIndexUnit.Normalized;
        dolly.CameraPosition = tNorm;

        // на один кадр отключаем демпфинг, чтобы не было микрошага
        bool prev = dolly.Damping.Enabled;
        dolly.Damping.Enabled = false;

        // сбрасываем внутренние кэши, чтобы Dolly стартовал с текущей позиции
        var vcam = GetComponent<CinemachineCamera>();
        vcam.PreviousStateIsValid = false;

        dolly.enabled = true;
        // вернуть демпфинг в следующем кадре
        StartCoroutine(ReenableDampingNextFrame(prev));
    }

    IEnumerator ReenableDampingNextFrame(bool value)
    {
        yield return null;
        dolly.Damping.Enabled = value;
    }

    // сглаживание параметра t после переключения, чтобы не было «догоняющего скачка»
    IEnumerator ParamHandoff(SplineContainer sc)
    {
        bool wasAuto = dolly.AutomaticDolly.Enabled;
        dolly.AutomaticDolly.Enabled = false;   // временно ручное управление параметром

        float tCur = dolly.CameraPosition;
        float endTime = Time.unscaledTime + paramHandoffTime;

        while (Time.unscaledTime < endTime)
        {
            yield return new WaitForEndOfFrame();

            float tDesired = NearestT01(sc, GetPredictedTargetPos());
            // экспоненциальное сглаживание к целевому t
            float k = 1f - Mathf.Exp(-paramHandoffResponsiveness * Time.unscaledDeltaTime);
            tCur = Mathf.Lerp(tCur, tDesired, k);
            dolly.CameraPosition = tCur;
        }

        // финальное выравнивание
        dolly.CameraPosition = NearestT01(sc, GetPredictedTargetPos());

        dolly.AutomaticDolly.Enabled = wasAuto; // возвращаем авто-долли
    }

    // ближайший параметр t (0..1) к worldPos для sc
    float NearestT01(SplineContainer sc, Vector3 worldPos)
    {
        var s = sc.Splines[0];
        float3 local = (float3)sc.transform.InverseTransformPoint(worldPos);
        SplineUtility.GetNearestPoint(s, local, out float3 _, out float t);
        return t; // совместим с PathIndexUnit.Normalized
    }

    // предсказанная позиция цели с горизонтом anticipateTime
    Vector3 GetPredictedTargetPos()
    {
        Vector3 pos = target ? target.position : transform.position;
        Vector2 vel;
        if (targetRb) vel = targetRb.linearVelocity;
        else
        {
            Vector2 cur = (Vector2)pos;
            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            vel = (cur - lastTargetPos) / dt;
            lastTargetPos = cur;
        }
        if (vel.magnitude >= minSpeedToAnticipate)
            pos += (Vector3)(vel * anticipateTime);
        return pos;
    }

    // расстояние до ближайшей точки сплайна (учёт локала контейнера), только XY
    static float DistanceSqrWorldXY(Vector3 worldPos, SplineContainer sc)
    {
        var s = sc.Splines[0];
        float3 localPos = (float3)sc.transform.InverseTransformPoint(worldPos);
        SplineUtility.GetNearestPoint(s, localPos, out float3 nearestLocal, out float _);
        Vector3 nearestWorld = sc.transform.TransformPoint((Vector3)nearestLocal);
        Vector2 a = new Vector2(worldPos.x,  worldPos.y);
        Vector2 b = new Vector2(nearestWorld.x, nearestWorld.y);
        return (a - b).sqrMagnitude;
    }
}
