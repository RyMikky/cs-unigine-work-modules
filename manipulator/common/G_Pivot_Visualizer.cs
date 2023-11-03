using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unigine;

[Component(PropertyGuid = "542cb47ff305b7d2ce78f509d815fad476312823")]
public class G_Pivot_Visualizer : Component
{
	
	public enum COLOR
	{
		RED, GREEN, YELLOW, BLUE, WHITE, BLACK
	}

	[ShowInEditor]
	private bool isEnable = false;

	[ShowInEditor]
	private float radius = 0.01f;

	[ShowInEditor]
	private COLOR color;

	private vec4 GetColor(COLOR color)
	{
		switch (color)
		{
			case COLOR.RED:
			return vec4.RED;

			case COLOR.GREEN:
			return vec4.GREEN;

			case COLOR.YELLOW:
			return vec4.YELLOW;

			case COLOR.BLUE:
			return vec4.BLUE;

			case COLOR.WHITE:
			return vec4.WHITE;

			case COLOR.BLACK:
			return vec4.BLACK;
		}

		return new vec4();
	}

	private void Update()
	{
		Visualizer.Enabled = isEnable;
		Visualizer.Mode=Visualizer.MODE.ENABLED_DEPTH_TEST_DISABLED;
		Visualizer.RenderSolidSphere(radius, node.WorldTransform, GetColor(color));
	}
}