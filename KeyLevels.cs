#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class KeyLevels : Indicator
	{
//		private int		ninja_Start_Time;
//		private int		ninja_End_Time;
//		private int		ninja_IB_End_Time;
		private int		latestRTHbar;
		private int		lastBar;
		private bool 	sunday = false;
		private int 	rthStartBarNum;
		private string 	yDate;
		private double	yHigh;
		private double	yLow;
		private int 	rthEndBarNum; 
		private NinjaTrader.Gui.Tools.SimpleFont myFont = new NinjaTrader.Gui.Tools.SimpleFont("Helvetica", 12) { Size = 12, Bold = false };
				
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Key Levels";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				LineColor					= Brushes.DimGray;
				VAColor					= Brushes.DodgerBlue;
				PocColor					= Brushes.Red;
				GapUp					= Brushes.LimeGreen;
				GapDown					= Brushes.Red;
				RTHOpen						= DateTime.Parse("08:30", System.Globalization.CultureInfo.InvariantCulture);
				RTHClose						= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				BarsRight					= 5;
				ShowRange					= true;
				RangeHighLevel					= 4000;
				RangeLowLevel					= 4000;
				RangePOCLevel					= 4000;
				RangeVAHLevel					= 4000;
				RangeVALLevel					= 4000;
				YesterdaysPOC					= 4000;
				AddPlot(Brushes.DimGray, "YHigh");
				AddPlot(Brushes.DimGray, "YLow");
				AddPlot(Brushes.DimGray, "OpenLine");
				AddPlot(Brushes.DimGray, "GXHigh");
				AddPlot(Brushes.DimGray, "GXLow");
				AddPlot(Brushes.Orange, "RangeHigh");
				AddPlot(Brushes.Orange, "RangeLow");
				AddPlot(Brushes.Orange, "RangePOC");
				AddPlot(Brushes.Orange, "RangeVAH");
				AddPlot(Brushes.Orange, "RangeVAL");
				AddPlot(Brushes.Orange, "YPOC");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				ClearOutputWindow();
			}
		}

		/*
			[ ] func Session Start, 
			[ ] func SessionEnd 
			[ ] func RTHSessionPlots 
			[X] func HollidayOrSunday
			
			[ ] plot Y hi low
			[ ] plot gx hi low
			[ ] plot open
			[ ] inputs too
			[ ] plot as optional Range Hi Lo VAH VAL POC
			[ ] Ploy Ypoc if naked 
			[ ] plot gap as box, red / green, num bars
		*/
		
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 20 ) { return; }
			lastBar = CurrentBar - 1;
			CheckHolidayOrSunday();
			if ( sunday  ) { return; }
			SessionStart();
			SessionEnd();
			RegularSession();
		}

		private void SessionStart() { 
			if (BarsInProgress == 1 && RTHOpen.ToLongTimeString() == Time[0].ToLongTimeString() ) {
				rthStartBarNum = CurrentBar ;
				Print("OPEN: " + RTHOpen + " == " +  Time[0] );
            }
		}
		
		private void SessionEnd() { 
			if (BarsInProgress == 1 && RTHClose.ToLongTimeString() == Time[0].ToLongTimeString() ) {
				rthEndBarNum = CurrentBar;
				Print("CLOSE: " + RTHClose + " == " +  Time[0] );
				// find RTH High + Low
				int rthLength = rthEndBarNum - rthStartBarNum;
				if ( rthLength > 0 ) {
	                yHigh = MAX(High, rthLength)[0];
	                yLow = MIN(Low, rthLength)[0];
				}
            }
		}
		
		private void RegularSession() {   
			if (IsBetween(start: ToTime(RTHOpen), end: ToTime(RTHClose))) {
				if (yHigh > 0.0 &&  yLow > 0.0) {
					YHigh[0] = yHigh; 
					YLow[0] = yLow; 					
					LineText(name: "yh", price: yHigh);
					LineText(name: "yl", price: yLow);
				} 
			}
		}
		
		private bool IsBetween(int start, int end) {
			var Now = ToTime(Time[0]) ;
			if (Now > start && Now < end) {
				return true;
			} else { return false; }
		}
		
		private void LineText(string name, double price) { 
			Draw.Text(this, name, false, "yh", -BarsRight, price, 0,  LineColor, myFont, TextAlignment.Center, Brushes.Transparent, LineColor, 0);
		}
		
		private void CheckHolidayOrSunday() { 
			if (Bars.IsFirstBarOfSession) {
				//NinjaTrader.Gui.Tools.SimpleFont myFont = new NinjaTrader.Gui.Tools.SimpleFont("Helvetica", 18) { Size = 18, Bold = false };
				DateTime myDate = Time[0];   
				string prettyDate = myDate.ToString("MM/d/yyyy");
				yDate = Time[0].DayOfWeek.ToString();
				if (yDate == "Sunday" ) {
					//Print(yDate + "  " + Time[0].ToShortDateString() );
					sunday = true;
					Draw.Text(this, "sunday"+prettyDate, true, yDate, 0, MAX(High, 20)[1], 1, Brushes.DarkGoldenrod, myFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 50);
				} else {
					sunday = false;	
				}
			}
			
			//MARK: - TODO - Holiday doesnt prinnt anything
			/*
			foreach(KeyValuePair<DateTime, string> holiday in TradingHours.Holidays)
			{
                string dateOnly = String.Format("{0:MM/dd/yyyy}", holiday.Key);
                
               // Print("dateOnly " + dateOnly + "   today " + prettyDate);
				
                if (dateOnly == prettyDate)
                {
                    Print("\nToday is " + holiday.Value + "\n");
					sunday = true;
                    //if (Bars.IsFirstBarOfSession)
                    //{ 
                        
                        Draw.Text(this, "holiday"+prettyDate, true, holiday.Value, 0, MAX(High, 20)[1], 1, Brushes.DarkGoldenrod, myFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 50);
						Print(holiday.Value + "  " + Time[0].ToShortDateString() );
                    //}
                }
			}
			*/
		}
		
		#region Properties
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="LineColor", Order=1, GroupName="Parameters")]
		public Brush LineColor
		{ get; set; }

		[Browsable(false)]
		public string LineColorSerializable
		{
			get { return Serialize.BrushToString(LineColor); }
			set { LineColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="VAColor", Order=2, GroupName="Parameters")]
		public Brush VAColor
		{ get; set; }

		[Browsable(false)]
		public string VAColorSerializable
		{
			get { return Serialize.BrushToString(VAColor); }
			set { VAColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="PocColor", Order=3, GroupName="Parameters")]
		public Brush PocColor
		{ get; set; }

		[Browsable(false)]
		public string PocColorSerializable
		{
			get { return Serialize.BrushToString(PocColor); }
			set { PocColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="GapUp", Order=4, GroupName="Parameters")]
		public Brush GapUp
		{ get; set; }

		[Browsable(false)]
		public string GapUpSerializable
		{
			get { return Serialize.BrushToString(GapUp); }
			set { GapUp = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="GapDown", Order=5, GroupName="Parameters")]
		public Brush GapDown
		{ get; set; }

		[Browsable(false)]
		public string GapDownSerializable
		{
			get { return Serialize.BrushToString(GapDown); }
			set { GapDown = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHOpen", Order=6, GroupName="Parameters")]
		public DateTime RTHOpen
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHClose", Order=7, GroupName="Parameters")]
		public DateTime RTHClose
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="BarsRight", Order=8, GroupName="Parameters")]
		public int BarsRight
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="ShowRange", Order=9, GroupName="Parameters")]
		public bool ShowRange
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="RangeHighLevel", Order=10, GroupName="Parameters")]
		public double RangeHighLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="RangeLowLevel", Order=11, GroupName="Parameters")]
		public double RangeLowLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="RangePOCLevel", Order=12, GroupName="Parameters")]
		public double RangePOCLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="RangeVAHLevel", Order=13, GroupName="Parameters")]
		public double RangeVAHLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="RangeVALLevel", Order=14, GroupName="Parameters")]
		public double RangeVALLevel
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="YesterdaysPOC", Order=15, GroupName="Parameters")]
		public double YesterdaysPOC
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YHigh
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YLow
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> OpenLine
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> GXHigh
		{
			get { return Values[3]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> GXLow
		{
			get { return Values[4]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeHigh
		{
			get { return Values[5]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeLow
		{
			get { return Values[6]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangePOC
		{
			get { return Values[7]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeVAH
		{
			get { return Values[8]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> RangeVAL
		{
			get { return Values[9]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YPOC
		{
			get { return Values[10]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private KeyLevels[] cacheKeyLevels;
		public KeyLevels KeyLevels(Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			return KeyLevels(Input, lineColor, vAColor, pocColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showRange, rangeHighLevel, rangeLowLevel, rangePOCLevel, rangeVAHLevel, rangeVALLevel, yesterdaysPOC);
		}

		public KeyLevels KeyLevels(ISeries<double> input, Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			if (cacheKeyLevels != null)
				for (int idx = 0; idx < cacheKeyLevels.Length; idx++)
					if (cacheKeyLevels[idx] != null && cacheKeyLevels[idx].LineColor == lineColor && cacheKeyLevels[idx].VAColor == vAColor && cacheKeyLevels[idx].PocColor == pocColor && cacheKeyLevels[idx].GapUp == gapUp && cacheKeyLevels[idx].GapDown == gapDown && cacheKeyLevels[idx].RTHOpen == rTHOpen && cacheKeyLevels[idx].RTHClose == rTHClose && cacheKeyLevels[idx].BarsRight == barsRight && cacheKeyLevels[idx].ShowRange == showRange && cacheKeyLevels[idx].RangeHighLevel == rangeHighLevel && cacheKeyLevels[idx].RangeLowLevel == rangeLowLevel && cacheKeyLevels[idx].RangePOCLevel == rangePOCLevel && cacheKeyLevels[idx].RangeVAHLevel == rangeVAHLevel && cacheKeyLevels[idx].RangeVALLevel == rangeVALLevel && cacheKeyLevels[idx].YesterdaysPOC == yesterdaysPOC && cacheKeyLevels[idx].EqualsInput(input))
						return cacheKeyLevels[idx];
			return CacheIndicator<KeyLevels>(new KeyLevels(){ LineColor = lineColor, VAColor = vAColor, PocColor = pocColor, GapUp = gapUp, GapDown = gapDown, RTHOpen = rTHOpen, RTHClose = rTHClose, BarsRight = barsRight, ShowRange = showRange, RangeHighLevel = rangeHighLevel, RangeLowLevel = rangeLowLevel, RangePOCLevel = rangePOCLevel, RangeVAHLevel = rangeVAHLevel, RangeVALLevel = rangeVALLevel, YesterdaysPOC = yesterdaysPOC }, input, ref cacheKeyLevels);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.KeyLevels KeyLevels(Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			return indicator.KeyLevels(Input, lineColor, vAColor, pocColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showRange, rangeHighLevel, rangeLowLevel, rangePOCLevel, rangeVAHLevel, rangeVALLevel, yesterdaysPOC);
		}

		public Indicators.KeyLevels KeyLevels(ISeries<double> input , Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			return indicator.KeyLevels(input, lineColor, vAColor, pocColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showRange, rangeHighLevel, rangeLowLevel, rangePOCLevel, rangeVAHLevel, rangeVALLevel, yesterdaysPOC);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.KeyLevels KeyLevels(Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			return indicator.KeyLevels(Input, lineColor, vAColor, pocColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showRange, rangeHighLevel, rangeLowLevel, rangePOCLevel, rangeVAHLevel, rangeVALLevel, yesterdaysPOC);
		}

		public Indicators.KeyLevels KeyLevels(ISeries<double> input , Brush lineColor, Brush vAColor, Brush pocColor, Brush gapUp, Brush gapDown, DateTime rTHOpen, DateTime rTHClose, int barsRight, bool showRange, double rangeHighLevel, double rangeLowLevel, double rangePOCLevel, double rangeVAHLevel, double rangeVALLevel, double yesterdaysPOC)
		{
			return indicator.KeyLevels(input, lineColor, vAColor, pocColor, gapUp, gapDown, rTHOpen, rTHClose, barsRight, showRange, rangeHighLevel, rangeLowLevel, rangePOCLevel, rangeVAHLevel, rangeVALLevel, yesterdaysPOC);
		}
	}
}

#endregion
