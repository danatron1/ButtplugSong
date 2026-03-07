using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace ButtplugSong.GUI.CustomUI;

[UxmlElement]
[Preserve]
public partial class WaveDisplay : VisualElement
{
    private LinkedList<float> dataPoints = new();

    [UxmlAttribute]
    public Color LineColor = Color.white;

    [UxmlAttribute]
    public float LineWidth = 2f;

    [UxmlAttribute]
    public Color GlowColor = Color.white;

    [UxmlAttribute]
    public float GlowWidth = 2f;

    private float _minValue = 0, _maxValue = 100, _defaultValue = 0;
    [UxmlAttribute]
    public float MinimumValue
    {
        get
        {
            return _minValue;
        }
        set
        {
            _minValue = value;
            MarkDirtyRepaint();
        }
    }

    [UxmlAttribute]
    public float MaximumValue
    {
        get
        {
            return _maxValue;
        }
        set
        {
            _maxValue = value;
            MarkDirtyRepaint();
        }
    }

    [UxmlAttribute]
    public float DefaultValue
    {
        get
        {
            return _defaultValue;
        }
        set
        {
            _defaultValue = value;
            MarkDirtyRepaint();
        }
    }

    [UxmlAttribute]
    public int RecordSteps = 100;


    [UxmlAttribute]
    public float LineBufferTop = 0f;
    [UxmlAttribute]
    public float LineBufferBottom = 0f;
    public WaveDisplay()
    {
        if (MinimumValue >= MaximumValue) throw new ArgumentException("Minimum value must be less than maximum value");
        ClearRecord();
        generateVisualContent += GenerateVisualContent;

    }
    public void ClearRecord()
    {
        for (int i = 0; i < RecordSteps; i++) PushRecordStep();
    }
    public void PushRecordStep(float? record = null)
    {
        dataPoints.AddFirst(record ?? DefaultValue);
        while (dataPoints.Count > RecordSteps) dataPoints.RemoveLast();
        MarkDirtyRepaint();
    }
    private void GenerateVisualContent(MeshGenerationContext mesh)
    {
        float height = contentRect.height - LineBufferBottom - LineBufferTop;
        float hStep = RecordSteps < 2 ? contentRect.width : contentRect.width / (RecordSteps - 1);

        Painter2D painter = mesh.painter2D;
        painter.lineWidth = GlowWidth + LineWidth;
        LinkedListNode<float> dataPoint = dataPoints.First;
        painter.BeginPath();
        painter.MoveTo(new Vector2(0, RecordToHeight(dataPoint.Value)));
        painter.strokeColor = GlowColor;
        for (int i = 1; i < RecordSteps; i++)
        {
            if (dataPoint.Next != null) dataPoint = dataPoint.Next;
            painter.LineTo(new Vector2(i * hStep, RecordToHeight(dataPoint.Value)));
        }
        painter.Stroke();
        painter.strokeColor = LineColor;
        painter.lineWidth = LineWidth;
        painter.Stroke();

        float RecordToHeight(float record)
        {
            if (record >= MaximumValue) return LineBufferTop;
            if (record <= MinimumValue) return LineBufferTop + height;
            return height * (1 - (record - MinimumValue) / (MaximumValue - MinimumValue)) + LineBufferTop;
        }
    }
}
