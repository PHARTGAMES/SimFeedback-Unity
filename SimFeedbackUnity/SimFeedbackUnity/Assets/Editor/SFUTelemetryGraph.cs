using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SFUTelemetry
{
    public class SFUTelemetryGraphWindow : EditorWindow
    {
        static float graphXStartMult = 0.025f;
        static float graphYStartMult = 1.0f;
        static float graphX = 0.0f;
        static float graphY = 0.0f;
        static float graphSpacingXMult = 0.025f;
        static float graphSpacingYMult = 1.0f;
        static float graphWidthMult = 1.0f;
        static float graphHeightMult = 0.5f;
        Vector2 scrollPosition = Vector2.zero;
        float scrollViewHeight = 100.0f;

        public class TelemetryGraphData
        {
            public float minValue;
            public float maxValue;
            public string valueName;
            public bool visible;
            FieldInfo fieldInfo;
            PropertyInfo propertyInfo;

            public TelemetryGraphData(string _valueName)
            {
                valueName = _valueName;
                minValue = -1.0f;
                maxValue = 1.0f;
                visible = false;
                propertyInfo = null;
                Type eleDataType = typeof(SFUAPI);
                fieldInfo = eleDataType.GetField(valueName);
                if (fieldInfo == null)
                {
                    propertyInfo = eleDataType.GetProperty(valueName);
                }
            }

            public void AdjustMinMax(List<SFUAPI> telemetryHistory)
            {
                foreach (SFUAPI api in telemetryHistory)
                {
                    float value = GetAPIValue(api);

                    minValue = Mathf.Min(minValue, value);
                    maxValue = Mathf.Max(maxValue, value);
                }
            }

            public float GetAPIValue(SFUAPI api)
            {
                float value = 0.0f;
                Type eleDataType = typeof(SFUAPI);
                     
                if(fieldInfo != null)
                {
                    value = (float)fieldInfo.GetValue(api);
                }
                else        
                if (propertyInfo != null)
                {
                    value = (float)propertyInfo.GetValue(api, null);
                }
                return value;
            }
        };

        public static TelemetryGraphData[] telemetryGraphs;

        [MenuItem("SFUTelemetry/Graphs")]
        public static void ShowWindow()
        {
            PopulateTelemetryGraphs();

            graphWidthMult = 0.5f - ((graphXStartMult) + (graphSpacingXMult));
            GetWindow<SFUTelemetryGraphWindow>(false, "SFUTelemetry Graphs", true);
        }

        private void ValidateState()
        {
            if (telemetryGraphs == null)
            {
                PopulateTelemetryGraphs();
                graphWidthMult = 0.5f - ((graphXStartMult) + (graphSpacingXMult));
            }
        }

        public static void PopulateTelemetryGraphs()
        {
            string[] valueList = SimFeedbackUnity.GetValueList();

            telemetryGraphs = new TelemetryGraphData[valueList.Length];
            for (int i = 0; i < telemetryGraphs.Length; ++i)
            {
                telemetryGraphs[i] = new TelemetryGraphData(valueList[i]);
            }
        }

        private void OnGUI()
        {
            ValidateState();

            graphX = position.width * graphXStartMult;
            graphY = graphX * graphYStartMult;
            float graphWidth = graphWidthMult * position.width;
            float graphHeight = graphWidth * graphHeightMult;
            float labelWidth = position.width * 0.1f;
            float labelHeight = 20.0f;

            int graphCount = telemetryGraphs.Length;
            int currGraph = 0;
            scrollPosition = GUI.BeginScrollView(new Rect(0.0f, 0.0f, position.width, position.height), scrollPosition, new Rect(0.0f, 0.0f, position.width, scrollViewHeight), false, true);

            scrollViewHeight = 100.0f;
            for (int y = 0; y < 512; ++y) //if there's more than 1024 graphs we gots problems
            {
                graphX = position.width * graphXStartMult;
                bool anyVisible = false;
                for (int x = 0; x < 2; ++x)
                {
                    Rect graphRect = new Rect(graphX, graphY, graphWidth, graphHeight);
                    TelemetryGraphData graphData = telemetryGraphs[currGraph];

                    if (!graphData.visible)
                    {
                        graphRect = new Rect(graphX, graphY, graphWidth, labelHeight);
                    }
                    else
                    {
                        anyVisible = true;
                    }

                    GUILayout.BeginArea(graphRect);
                    GUILayout.Box("", GUILayout.Width(graphWidth), GUILayout.Height(graphHeight));


                    //draw markings
                    Handles.color = Color.grey;
                    Handles.DrawLine(new Vector3(0.0f, graphHeight * 0.5f, 0.0f), new Vector3(graphWidth, graphHeight * 0.5f, 0));

                    //draw label
                    GUILayout.BeginArea(new Rect((graphWidth * 0.5f) - (labelWidth * 0.5f), 0.0f, labelWidth, labelHeight));
                    GUILayout.Label(graphData.valueName, GUILayout.Width(labelWidth), GUILayout.Height(labelHeight));
                    GUILayout.EndArea();
                    float buttonWidth = labelHeight;
                    float buttonHeight = labelHeight;
                    GUILayout.BeginArea(new Rect(graphRect.width-buttonWidth, 0.0f, buttonWidth, labelHeight));
                    if (GUILayout.Button(graphData.visible ? "X" : "O", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                    {
                        graphData.visible = !graphData.visible;
                        SortGraphData();
                    }
                    GUILayout.EndArea();

                    if (graphData.visible)
                    {
                        //draw max value
                        GUILayout.BeginArea(new Rect(0.0f, 0.0f, labelWidth, labelHeight));
                        GUILayout.Label("" + graphData.maxValue, GUILayout.Width(labelWidth), GUILayout.Height(labelHeight));
                        GUILayout.EndArea();


                        //draw min value
                        GUILayout.BeginArea(new Rect(0.0f, graphHeight - labelHeight, labelWidth, labelHeight));
                        GUILayout.Label("" + graphData.minValue, GUILayout.Width(labelWidth), GUILayout.Height(labelHeight));
                        GUILayout.EndArea();

                        if (SimFeedbackUnity.Instance != null)
                        {
                            graphData.AdjustMinMax(SimFeedbackUnity.Instance.GetRawTelemetryHistory());
                            graphData.AdjustMinMax(SimFeedbackUnity.Instance.GetFilteredTelemetryHistory());
                            DrawGraph(graphData, SimFeedbackUnity.Instance.GetRawTelemetryHistory(), Color.red, graphWidth, graphHeight);
                            DrawGraph(graphData, SimFeedbackUnity.Instance.GetFilteredTelemetryHistory(), Color.green, graphWidth, graphHeight);
                        }
                    }
                    GUILayout.EndArea();

                    graphX += graphWidth + (graphSpacingXMult * position.width);
                    graphCount--;
                    if (graphCount <= 0)
                        break;
                    currGraph++;
                }
                if (graphCount <= 0)
                    break;
                float yHeightIncrement;
                if (anyVisible)
                {
                    yHeightIncrement = graphHeight + ((graphSpacingXMult * position.width) * graphSpacingYMult);
                }
                else
                {
                    yHeightIncrement = labelHeight + ((graphSpacingXMult * position.width) * graphSpacingYMult);
                }
                graphY += yHeightIncrement;
                scrollViewHeight += yHeightIncrement;
            }
            GUI.EndScrollView();
        }


        public int CompareGraphData(TelemetryGraphData a, TelemetryGraphData b)
        {
            if (a.visible && b.visible || !a.visible && !b.visible)
                return 0;

            if (!a.visible && b.visible)
                return 1;

            return -1;
        }

        void SortGraphData()
        {
            Array.Sort(telemetryGraphs, CompareGraphData);
        }

        void DrawGraph(TelemetryGraphData graphData, List<SFUAPI> telemetryHistory, Color color, float width, float height)
        {

            float graphPosX = width;
            float graphPosY = height * 0.5f;

            int xSteps = SimFeedbackUnity.maxTelemetryHistory;
            float xStepSize = width / (float)xSteps;

            float lastGraphPosX = graphPosX;
            float lastGraphPosY = height * 0.5f;

            float yScale = Mathf.Max(Mathf.Abs(graphData.maxValue), Mathf.Abs(graphData.minValue));

            for (int x = SimFeedbackUnity.maxTelemetryHistory; x >= 0; x--)
            {
                graphPosY = height * 0.5f;

                if (x < telemetryHistory.Count)
                {
                    graphPosY = (height * 0.5f) + ((-graphData.GetAPIValue(telemetryHistory[x]) / yScale) * (height * 0.5f));
                }

                if(x == telemetryHistory.Count-1)
                {
                    lastGraphPosY = graphPosY;
                }

                Handles.color = color;
                Handles.DrawLine(new Vector3(lastGraphPosX, lastGraphPosY, 0.0f), new Vector3(graphPosX, graphPosY, 0));
                lastGraphPosX = graphPosX;
                lastGraphPosY = graphPosY;
                graphPosX -= xStepSize;
            }
        }
        private void Update()
        {
            Repaint();
        }
    }

}
