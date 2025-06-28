using System;
using Gestures.Controls;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gestures.View
{
    public class GestureView : MonoBehaviour
    {
        [SerializeField] private float brushRadius;
        [SerializeField] private GameObject input;
        [SerializeField] private ComputeShader drawShader;
        [SerializeField] private RenderTexture outputTexture;
        [SerializeField] private Color brushColor;

        public Color BrushColor
        {
            get => brushColor;
            set
            {
                brushColor = value;
                RefreshBrushColor();
            }
        }
        
        private IVectorArrayProcessor _input;
        private int _drawKernelIndex;
        private int _clearKernelIndex;
        private int _bakeKernelIndex;
        private int3 _drawKernelThreadGroups;
        private int3 _clearKernelThreadGroups;
        private int3 _bakeKernelThreadGroups;
        private bool _initialized;
        private bool _bakeState;
        private bool _needBake;

        private static readonly int main_tex = Shader.PropertyToID("main_tex");
        private static readonly int double_tex = Shader.PropertyToID("double_tex");
        private static readonly int threads_count = Shader.PropertyToID("threads_count");
        private static readonly int canvas_size = Shader.PropertyToID("canvas_size");
        private static readonly int start_draw = Shader.PropertyToID("start_draw");
        private static readonly int end_draw = Shader.PropertyToID("end_draw");
        private static readonly int brush_radius = Shader.PropertyToID("brush_radius");
        private static readonly int brush_color = Shader.PropertyToID("brush_color");

        public void SetBrushState(bool value)
        {
            if (!_bakeState && value)
            {
                _needBake = true;
            }
            _bakeState = value;
        }
        
        private void Awake()
        {
            _input = input.GetComponent<IVectorArrayProcessor>();
            _input.OnInputComplete += Clear;
            _input.OnPointPlaced += UpdatePoint;
            _initialized = true;
            PrepareShader();
        }

        private void OnDestroy()
        {
            _initialized = false;
            _input.OnInputComplete -= Clear;
            _input.OnPointPlaced -= UpdatePoint;
        }

        private void OnValidate()
        {
            PrepareShader();
        }

        private void PrepareShader()
        {
            if (!_initialized)
            {
                return;
            }

            drawShader.SetVector(threads_count, new Vector4(outputTexture.width, outputTexture.height, 0, 0));
            Vector2 rectSize = (transform as RectTransform).rect.size;
            drawShader.SetVector(canvas_size, new Vector4(rectSize.x, rectSize.y, 0, 0));
            drawShader.SetFloat(brush_radius, brushRadius);
            RefreshBrushColor();
            _drawKernelIndex = drawShader.FindKernel("Draw");
            _bakeKernelIndex = drawShader.FindKernel("Bake");
            _clearKernelIndex = drawShader.FindKernel("Clear");
            drawShader.SetTexture(_drawKernelIndex, main_tex, outputTexture);
            drawShader.SetTexture(_bakeKernelIndex, main_tex, outputTexture);
            drawShader.SetTexture(_clearKernelIndex, main_tex, outputTexture);

            uint3 threadGroupSize = new uint3();
            drawShader.GetKernelThreadGroupSizes(_drawKernelIndex, out threadGroupSize.x, out threadGroupSize.y, out threadGroupSize.z);
            _drawKernelThreadGroups = (int3)math.ceil(new float3(outputTexture.width, outputTexture.height, 1) / (float3)threadGroupSize);
            drawShader.GetKernelThreadGroupSizes(_clearKernelIndex, out threadGroupSize.x, out threadGroupSize.y, out threadGroupSize.z);
            _clearKernelThreadGroups = (int3)math.ceil(new float3(outputTexture.width, outputTexture.height, 1) / (float3)threadGroupSize);
            drawShader.GetKernelThreadGroupSizes(_clearKernelIndex, out threadGroupSize.x, out threadGroupSize.y, out threadGroupSize.z);
            _bakeKernelThreadGroups = (int3)math.ceil(new float3(outputTexture.width, outputTexture.height, 1) / (float3)threadGroupSize);
        }

        private void RefreshBrushColor()
        {
            drawShader.SetVector(brush_color, brushColor);
        }

        private void UpdatePoint()
        {
            Draw(_needBake);
            _needBake = false;
        }
        
        private void Draw(bool bake)
        {
            if (_input.GetPointsCount() < 2)
            {
                return;
            }
            Vector2 a = _input.GetPoint(_input.GetPointsCount() - 1);
            Vector2 b = _input.GetPoint(_input.GetPointsCount() - 2);
            drawShader.SetVector(start_draw, new Vector4(a.x, a.y, 0, 0));
            drawShader.SetVector(end_draw, new Vector4(b.x, b.y, 0, 0));
            int3 threadGroups = bake ? _bakeKernelThreadGroups : _drawKernelThreadGroups;
            drawShader.Dispatch(bake ? _bakeKernelIndex : _drawKernelIndex, threadGroups.x, threadGroups.y, threadGroups.z);
        }

        private void Clear()
        {
            drawShader.Dispatch(_clearKernelIndex, _clearKernelThreadGroups.x, _clearKernelThreadGroups.y, _clearKernelThreadGroups.z);
        }
    }
}