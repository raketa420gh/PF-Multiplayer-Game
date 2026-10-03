using System;
using UnityEngine;

namespace Game.Scripts.Battle
{
    [Flags]
    public enum BodyRegion
    {
        None = 0,
        Head = 1,
        Torso = 2,
        UpperArms = 4,
        Forearms = 8,
        Hands = 16,
        Thighs = 32,
        Calves = 64,
        Feet = 128
    }

    public enum OutfitPart : byte
    {
        PeasantBody,
        PeasantArms,
        PeasantLegs,
        PeasantFeet,
        RangerBody,
        RangerArms,
        RangerBracers,
        RangerBelt1,
        RangerBelt2,
        RangerPauldron,
        RangerHood,
        RangerLegs,
        RangerBoots
    }

    /// Modular character: mannequin body regions and skinned outfit parts on one skeleton.
    /// A shown part hides the body regions it covers. Material slot 0 is the tintable one, slot 1 is skin or joints.
    public sealed class CharacterModelComponent : MonoBehaviour
    {
        public const int RegionCount = 8;

        public bool IsFemale => _isFemale;

        [SerializeField]
        private bool _isFemale;

        [SerializeField]
        private Renderer[] _maleBody;

        [SerializeField]
        private Renderer[] _femaleBody;

        [SerializeField]
        private Renderer[] _maleParts;

        [SerializeField]
        private Renderer[] _femaleParts;

        [SerializeField]
        private BodyRegion[] _covers;

        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock _block;
        private BodyRegion _hidden;

        public void SetFemale(bool isFemale)
        {
            _isFemale = isFemale;
            Clear();
        }

        public void Clear()
        {
            foreach (Renderer part in _maleParts)
                part.gameObject.SetActive(false);

            foreach (Renderer part in _femaleParts)
                part.gameObject.SetActive(false);

            _hidden = BodyRegion.None;
            ApplyBody();
        }

        public void Show(OutfitPart part, Material material, Color tint)
        {
            Renderer renderer = (_isFemale ? _femaleParts : _maleParts)[(int)part];
            renderer.gameObject.SetActive(true);

            if (material != null)
            {
                Material[] materials = renderer.sharedMaterials;
                materials[0] = material;
                renderer.sharedMaterials = materials;
            }

            SetColor(renderer, tint);
            _hidden |= _covers[(int)part];
            ApplyBody();
        }

        public void SetBodyColor(Color color)
        {
            foreach (Renderer region in _maleBody)
                SetColor(region, color);

            foreach (Renderer region in _femaleBody)
                SetColor(region, color);
        }

        /// Replaces the mannequin surface and the bare skin of every part, e.g. bone or rotten flesh for monsters.
        public void SetBodyMaterial(Material material)
        {
            foreach (Renderer[] renderers in new[] { _maleBody, _femaleBody })
            {
                foreach (Renderer region in renderers)
                    region.sharedMaterials = new[] { material, material };
            }

            foreach (Renderer[] renderers in new[] { _maleParts, _femaleParts })
            {
                foreach (Renderer part in renderers)
                {
                    Material[] materials = part.sharedMaterials;

                    if (materials.Length < 2)
                        continue;

                    materials[1] = material;
                    part.sharedMaterials = materials;
                }
            }
        }

        private void ApplyBody()
        {
            for (int i = 0; i < _maleBody.Length; i++)
            {
                bool isShown = (_hidden & (BodyRegion)(1 << i)) == 0;
                _maleBody[i].gameObject.SetActive(isShown && !_isFemale);
                _femaleBody[i].gameObject.SetActive(isShown && _isFemale);
            }
        }

        private void SetColor(Renderer renderer, Color color)
        {
            _block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_block, 0);
            _block.SetColor(s_baseColor, color);
            renderer.SetPropertyBlock(_block, 0);
        }
    }
}
