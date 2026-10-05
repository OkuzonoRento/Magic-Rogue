using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicRogue
{
    public class DamageFlash : MonoBehaviour
    {
        [Header("フラッシュ設定")]
        [SerializeField] private Color flashColor = Color.red;
        [SerializeField] private float flashDuration = 0.15f; // 点滅時間（秒）

        private List<Renderer> renderers = new List<Renderer>();
        private List<MaterialPropertyBlock> propertyBlocks = new List<MaterialPropertyBlock>();
        private Coroutine flashCoroutine;

        private void Awake()
        {
            // 子・孫オブジェクトに含まれる全ての Renderer (MeshRenderer, SkinnedMeshRenderer 等) を再帰的に取得
            GetComponentsInChildren(true, renderers);

            for (int i = 0; i < renderers.Count; i++)
            {
                propertyBlocks.Add(new MaterialPropertyBlock());
            }
        }

        /// <summary>
        /// 被弾時に外部から呼び出すフラッシュ処理
        /// </summary>
        public void CallDamageFlash()
        {
            if (renderers.Count == 0)
            {
                // 万が一Awake後に動的にモデルが生成された場合のリトライ
                GetComponentsInChildren(true, renderers);
                propertyBlocks.Clear();
                for (int i = 0; i < renderers.Count; i++) propertyBlocks.Add(new MaterialPropertyBlock());
            }

            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            SetColor(flashColor);

            yield return new WaitForSeconds(flashDuration);

            ResetColor();
            flashCoroutine = null;
        }

        private void SetColor(Color color)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null) continue;

                renderers[i].GetPropertyBlock(propertyBlocks[i]);

                // 多彩なシェーダーに対応できるよう主要なカラープロパティすべてに設定
                propertyBlocks[i].SetColor("_BaseColor", color); // URP Lit / Unlit
                propertyBlocks[i].SetColor("_Color", color);     // Standard / Built-in
                propertyBlocks[i].SetColor("_EmissionColor", color * 0.5f); // 発光対応

                renderers[i].SetPropertyBlock(propertyBlocks[i]);
            }
        }

        private void ResetColor()
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null) continue;

                // PropertyBlock をリセットして元のマテリアル状態に戻す
                renderers[i].SetPropertyBlock(null);
            }
        }
    }
}