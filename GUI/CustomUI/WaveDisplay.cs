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
    private LinkedList<WavePoint> dataPoints = new();

    [UxmlAttribute]
    public float WindowDuration = 5f;
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
    public float LineBufferTop = 0f;
    [UxmlAttribute]
    public float LineBufferBottom = 0f;
    public WaveDisplay()
    {
        if (MinimumValue >= MaximumValue) throw new ArgumentException("Minimum value must be less than maximum value");
        generateVisualContent += GenerateVisualContent;
    }
    public void PushRecordStep(float? record = null, bool repaint = true)
    {
        dataPoints.AddFirst(new WavePoint(record ?? DefaultValue, Time.time));
        while (dataPoints.Count > 0 && (Time.time - dataPoints.Last.Value.Timestamp) > WindowDuration)
        {
            dataPoints.RemoveLast();
        }
        MarkDirtyRepaint();
    }
    private void GenerateVisualContent(MeshGenerationContext mesh)
    {
        if (dataPoints.Count == 0) return;

        float height = contentRect.height - LineBufferBottom - LineBufferTop;
        float width = contentRect.width;
        float currentTime = Time.time;

        Painter2D painter = mesh.painter2D;
        painter.lineWidth = GlowWidth + LineWidth;

        painter.lineJoin = LineJoin.Round; //fix for the stupid visual glitch hopefully finally
        painter.lineCap = LineCap.Round;

        var node = dataPoints.First;
        painter.BeginPath();
        painter.MoveTo(new Vector2(GetXPos(node.Value.Timestamp), RecordToHeight(node.Value.Value)));

        painter.strokeColor = GlowColor;

        while (node.Next != null)
        {
            node = node.Next;
            painter.LineTo(new Vector2(GetXPos(node.Value.Timestamp), RecordToHeight(node.Value.Value)));
        }

        painter.Stroke();
        painter.strokeColor = LineColor;
        painter.lineWidth = LineWidth;
        painter.Stroke();

        float GetXPos(float timestamp) => width * ((currentTime - timestamp) / WindowDuration);

        float RecordToHeight(float record)
        {
            if (record >= MaximumValue) return LineBufferTop;
            if (record <= MinimumValue) return LineBufferTop + height;
            return height * (1 - (record - MinimumValue) / (MaximumValue - MinimumValue)) + LineBufferTop;
        }
    }
    /*
    private void GenerateVisualContent(MeshGenerationContext mesh)
    {
        double height = (double)contentRect.height - LineBufferBottom - LineBufferTop;
        double hStep = RecordSteps < 2 ? contentRect.width : (double)contentRect.width / (RecordSteps - 1);

        Painter2D painter = mesh.painter2D;
        painter.lineWidth = GlowWidth + LineWidth;
        LinkedListNode<double> dataPoint = dataPoints.First;
        painter.BeginPath();
        painter.MoveTo(new Vector2(0, (float)RecordToHeight(dataPoint.Value)));
        painter.strokeColor = GlowColor;
        for (int i = 1; i < RecordSteps; i++)
        {
            if (dataPoint.Next != null) dataPoint = dataPoint.Next;
            painter.LineTo(new Vector2((float)(hStep * i), (float)RecordToHeight(dataPoint.Value)));
        }
        painter.Stroke();
        painter.strokeColor = LineColor;
        painter.lineWidth = LineWidth;
        painter.Stroke();

        double RecordToHeight(double record)
        {
            if (record >= MaximumValue) return LineBufferTop;
            if (record <= MinimumValue) return LineBufferTop + height;
            return height * (1 - (record - MinimumValue) / (MaximumValue - MinimumValue)) + LineBufferTop;
        }
    }*/
}
public struct WavePoint(float value, float timestamp)
{
    public float Value = value;
    public float Timestamp = timestamp;
}