using DG.Tweening;
using UnityEngine;

namespace MyFramework.Util
{
    public static class DOTWeenUtil
    {
        #region Scale

        /// <summary>
        ///     DOTween缩放动画，最终缩放为 原始缩放 * 目标缩放
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="originalScale">原始缩放</param>
        /// <param name="targetScale">目标缩放</param>
        /// <param name="scaleDuration">缩放时间</param>
        public static void AnimateScale(GameObject target, float originalScale, float targetScale, float scaleDuration)
        {
            if (target == null) return;

            // 先停止该物体上的所有缩放动画
            DOTween.Kill(target.transform);

            // 使用DOTween创建缩放动画
            target.transform.DOScale(originalScale * targetScale, scaleDuration)
                .SetEase(Ease.OutQuad) // 设置缓动函数，让动画更自然
                .SetUpdate(true); // 即使时间缩放为0也能播放
        }

        /// <summary>
        ///     使用Vector3版本的缩放动画
        /// </summary>
        public static void AnimateScale(GameObject target, Vector3 originalScale, float targetScaleMultiplier,
            float scaleDuration)
        {
            if (target == null) return;

            // 先停止该物体上的所有缩放动画
            DOTween.Kill(target.transform);

            target.transform.DOScale(originalScale * targetScaleMultiplier, scaleDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
        
        /// <summary>
        ///     动画还原缩放 - 将物体平滑缩放回原始大小（float版本）
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="originalScale">原始缩放值</param>
        /// <param name="duration">还原动画时长</param>
        public static void RestoreScale(GameObject target, float originalScale, float duration)
        {
            if (target == null) return;

            DOTween.Kill(target.transform);

            target.transform.DOScale(originalScale, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        /// <summary>
        ///     动画还原缩放 - 将物体平滑缩放回原始大小（Vector3版本）
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="originalScale">原始缩放向量</param>
        /// <param name="duration">还原动画时长</param>
        public static void RestoreScale(GameObject target, Vector3 originalScale, float duration)
        {
            if (target == null) return;

            DOTween.Kill(target.transform);

            target.transform.DOScale(originalScale, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        /// <summary>
        ///     立即停止缩放动画并还原到原始缩放（float版本）
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="originalScale">原始缩放值</param>
        public static void ResetScaleImmediate(GameObject target, float originalScale)
        {
            if (target == null) return;

            DOTween.Kill(target.transform);
            target.transform.localScale = Vector3.one * originalScale;
        }

        /// <summary>
        ///     立即停止缩放动画并还原到原始缩放（Vector3版本）
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="originalScale">原始缩放向量</param>
        public static void ResetScaleImmediate(GameObject target, Vector3 originalScale)
        {
            if (target == null) return;

            DOTween.Kill(target.transform);
            target.transform.localScale = originalScale;
        }
        #endregion

        #region Rotation

        /// <summary>
        ///     DOTween旋转动画 - 持续旋转
        /// </summary>
        /// <param name="target">目标Transform</param>
        /// <param name="speed">旋转速度（度/秒）</param>
        /// <param name="direction">旋转方向（1=顺时针，-1=逆时针）</param>
        /// <param name="loopType">循环类型</param>
        /// <param name="ease">缓动类型</param>
        public static Tween AnimateRotation(
            Transform target,
            float speed,
            int direction = 1,
            LoopType loopType = LoopType.Restart,
            Ease ease = Ease.Linear)
        {
            if (target == null) return null;

            // 停止该物体上的所有旋转动画
            DOTween.Kill(target);

            // 计算旋转一圈所需的时间
            var duration = Mathf.Abs(360f / speed);
            var targetAngle = direction > 0 ? 360f : -360f;

            // 重置到0度
            var currentEuler = target.localEulerAngles;
            target.localEulerAngles = new Vector3(currentEuler.x, currentEuler.y, 0f);

            // 创建旋转动画
            return target
                .DORotate(new Vector3(0, 0, targetAngle), duration, RotateMode.FastBeyond360)
                .SetEase(ease)
                .SetLoops(-1, loopType)
                .SetUpdate(true);
        }

        /// <summary>
        ///     DOTween旋转动画 - 旋转到指定角度
        /// </summary>
        /// <param name="target">目标Transform</param>
        /// <param name="targetAngle">目标角度</param>
        /// <param name="duration">动画时长</param>
        /// <param name="ease">缓动类型</param>
        public static Tween AnimateRotationToAngle(
            Transform target,
            float targetAngle,
            float duration,
            Ease ease = Ease.OutQuad)
        {
            if (target == null) return null;

            // 停止该物体上的所有旋转动画
            DOTween.Kill(target);

            return target
                .DORotate(new Vector3(0, 0, targetAngle), duration, RotateMode.FastBeyond360)
                .SetEase(ease)
                .SetUpdate(true);
        }

        /// <summary>
        ///     停止指定目标的旋转动画
        /// </summary>
        /// <param name="target">目标Transform</param>
        public static void StopRotation(Transform target)
        {
            if (target == null) return;
            DOTween.Kill(target);
        }

        /// <summary>
        ///     暂停指定目标的旋转动画
        /// </summary>
        /// <param name="target">目标Transform</param>
        public static void PauseRotation(Transform target)
        {
            if (target == null) return;
            DOTween.Pause(target);
        }

        /// <summary>
        ///     恢复指定目标的旋转动画
        /// </summary>
        /// <param name="target">目标Transform</param>
        public static void ResumeRotation(Transform target)
        {
            if (target == null) return;
            DOTween.Play(target);
        }

        #endregion
    }
}