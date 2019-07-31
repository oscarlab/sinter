using Sintering;
using System;
using System.Linq;
using System.Windows.Automation;

namespace WindowsScraper
{

    public partial class WindowsScraper
    {
        
        public void FetchPropertiesFromAutomationElement(AutomationElement element, ref Entity entity)
        {
            entity.DetailedEntity = new Sintering.PlatformSpecificInfo.DetailedEntity();

            #region Properties
            try
            {
                 entity.DetailedEntity.AcceleratorKeyProperty = element.Current.AcceleratorKey;
            }
            catch
            {
                entity.DetailedEntity.AcceleratorKeyProperty = "";
            }

            try
            {
                entity.DetailedEntity.AccessKeyProperty = element.Current.AccessKey;
            }
            catch
            {
                entity.DetailedEntity.AccessKeyProperty = "";
            }

            try
            {
                entity.DetailedEntity.AutomationIdProperty = element.Current.AutomationId;
            }
            catch
            {
                entity.DetailedEntity.AutomationIdProperty = "";
            }
            try
            {
                entity.DetailedEntity.ClassNameProperty = element.Current.ClassName;
            }
            catch
            {
                entity.DetailedEntity.ClassNameProperty = "";
            }

            try
            {
                entity.DetailedEntity.HelpTextProperty = element.Current.HelpText;
            }
            catch
            {
                entity.DetailedEntity.HelpTextProperty = "";
            }


            // clickable point
            entity.DetailedEntity.ClickablePointProperty = "";
            object _ClickablePointProperty = element.GetCurrentPropertyValue(AutomationElement.ClickablePointProperty);
            if (_ClickablePointProperty is System.Windows.Point)
                entity.DetailedEntity.ClickablePointProperty = ((System.Windows.Point)_ClickablePointProperty).ToString();

            entity.DetailedEntity.ControlTypeProperty = element.Current.ControlType.ProgrammaticName;

            // culture
            entity.DetailedEntity.CultureProperty = "";
            var culture = element.GetCurrentPropertyValue(AutomationElement.CultureProperty);
            if (culture is System.Globalization.CultureInfo)
                entity.DetailedEntity.CultureProperty = ((System.Globalization.CultureInfo)culture).ToString();

            entity.DetailedEntity.FrameworkIdProperty = element.Current.FrameworkId;
            entity.DetailedEntity.HasKeyboardFocusProperty = element.Current.HasKeyboardFocus.ToString();
            entity.DetailedEntity.IsContentElementProperty = element.Current.IsContentElement.ToString();
            entity.DetailedEntity.IsControlElementProperty = element.Current.IsControlElement.ToString();
            entity.DetailedEntity.IsEnabledProperty = element.Current.IsEnabled.ToString();
            entity.DetailedEntity.IsKeyboardFocusableProperty = element.Current.IsKeyboardFocusable.ToString();
            entity.DetailedEntity.IsOffscreenProperty = element.Current.IsOffscreen.ToString();
            entity.DetailedEntity.IsPasswordProperty = element.Current.IsPassword.ToString();
            entity.DetailedEntity.IsRequiredForFormProperty = element.Current.IsRequiredForForm.ToString();
            entity.DetailedEntity.ItemStatusProperty = element.Current.ItemStatus;
            entity.DetailedEntity.ItemTypeProperty = element.Current.ItemType;
            entity.DetailedEntity.LocalizedControlTypeProperty = element.Current.LocalizedControlType;

            entity.DetailedEntity.LabeledByProperty = "";
            if (element.Current.LabeledBy != null)
                entity.DetailedEntity.LabeledByProperty = string.Join("/", element.Current.LabeledBy.GetRuntimeId());

            entity.DetailedEntity.NameProperty = element.Current.Name;

            entity.DetailedEntity.NativeWindowHandleProperty = element.Current.NativeWindowHandle.ToString();
            if (entity.DetailedEntity.NativeWindowHandleProperty == "0")
                entity.DetailedEntity.NativeWindowHandleProperty = "";

            entity.DetailedEntity.OrientationProperty = element.Current.Orientation.ToString();
            entity.DetailedEntity.ProcessIdProperty = ""; // element.Current.ProcessId.ToString();
            entity.DetailedEntity.RuntimeIdProperty = string.Join("/", element.GetRuntimeId());

            entity.DetailedEntity.IsDockPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsDockPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsExpandCollapsePatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsExpandCollapsePatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsGridItemPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsGridItemPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsGridPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsGridPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsInvokePatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsInvokePatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsItemContainerPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsItemContainerPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsItemContainerPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsItemContainerPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsMultipleViewPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsMultipleViewPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsRangeValuePatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsRangeValuePatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsScrollItemPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsScrollItemPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsScrollPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsScrollPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsSelectionItemPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsSelectionItemPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsSelectionPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsSelectionPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsSynchronizedInputPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsSynchronizedInputPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsTableItemPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsTableItemPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsTablePatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsTablePatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsTextPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsTextPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsTogglePatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsTogglePatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsTransformPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsTransformPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsValuePatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsValuePatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsVirtualizedItemPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsVirtualizedItemPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsWindowPatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsWindowPatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            entity.DetailedEntity.IsLegacyIAccessiblePatternAvailableProperty = (bool)element.GetCurrentPropertyValue(AutomationElementIdentifiers.IsLegacyIAccessiblePatternAvailableProperty) ? Boolean.TrueString : Boolean.FalseString;
            
            #endregion

            #region Patterns
            if (Boolean.Parse(entity.DetailedEntity.IsExpandCollapsePatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.ExpandCollapsePattern entity_pattern = new Sintering.PlatformSpecificInfo.ExpandCollapsePattern();
                System.Windows.Automation.ExpandCollapsePattern uia_pattern = (System.Windows.Automation.ExpandCollapsePattern)element.GetCurrentPattern(ExpandCollapsePatternIdentifiers.Pattern);
                entity.DetailedEntity.ExpandCollapsePatternNode = entity_pattern;
                entity_pattern.ExpandCollapseStateProperty = uia_pattern.Current.ExpandCollapseState.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsScrollItemPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.ScrollItemPattern entity_pattern = new Sintering.PlatformSpecificInfo.ScrollItemPattern();
                System.Windows.Automation.ScrollItemPattern uia_pattern = (System.Windows.Automation.ScrollItemPattern)element.GetCurrentPattern(ScrollItemPatternIdentifiers.Pattern);
                entity.DetailedEntity.ScrollItemPatternNode = entity_pattern;
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsValuePatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.ValuePattern entity_pattern = new Sintering.PlatformSpecificInfo.ValuePattern();
                System.Windows.Automation.ValuePattern uia_pattern = (System.Windows.Automation.ValuePattern)element.GetCurrentPattern(ValuePatternIdentifiers.Pattern);
                entity.DetailedEntity.ValuePatternNode = entity_pattern;
                entity_pattern.IsReadOnlyProperty = uia_pattern.Current.IsReadOnly.ToString();
                entity_pattern.ValueProperty = uia_pattern.Current.Value.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsSelectionPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.SelectionPattern entity_pattern = new Sintering.PlatformSpecificInfo.SelectionPattern();
                System.Windows.Automation.SelectionPattern uia_pattern = (System.Windows.Automation.SelectionPattern)element.GetCurrentPattern(SelectionPatternIdentifiers.Pattern);
                entity.DetailedEntity.SelectionPatternNode = entity_pattern;
                entity_pattern.CanSelectMultipleProperty = uia_pattern.Current.CanSelectMultiple.ToString();
                entity_pattern.IsSelectionRequiredProperty = uia_pattern.Current.IsSelectionRequired.ToString();
                entity_pattern.SelectionProperty = string.Join(",", uia_pattern.Current.GetSelection().Select(x => string.Join("/", x.GetRuntimeId())));

                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsTextPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.TextPattern entity_pattern = new Sintering.PlatformSpecificInfo.TextPattern();
                System.Windows.Automation.TextPattern uia_pattern = (System.Windows.Automation.TextPattern)element.GetCurrentPattern(TextPatternIdentifiers.Pattern);
                entity.DetailedEntity.TextPatternNode = entity_pattern;

                //entity_pattern.CultureAttribute = uia_pattern.DocumentRange.GetAttributeValue(System.Windows.Automation.TextPattern.CultureAttribute).ToString();

                //entity_pattern.DocumentRange = uia_pattern.Current.DocumentRange.ToString();
                //entity_pattern.SupportedTextSelection = uia_pattern.Current.SupportedTextSelection.ToString();
                //entity_pattern.AnimationStyleAttribute = uia_pattern.Current.AnimationStyleAttribute.ToString();
                //entity_pattern.BackgroundColorAttribute = uia_pattern.Current.BackgroundColorAttribute.ToString();
                //entity_pattern.BulletStyleAttribute = uia_pattern.Current.BulletStyleAttribute.ToString();
                //entity_pattern.CapStyleAttribute = uia_pattern.Current.CapStyleAttribute.ToString();
                //entity_pattern.CultureAttribute = uia_pattern.Current.CultureAttribute.ToString();
                //entity_pattern.FontNameAttribute = uia_pattern.Current.FontNameAttribute.ToString();
                //entity_pattern.FontSizeAttribute = uia_pattern.Current.FontSizeAttribute.ToString();
                //entity_pattern.FontWeightAttribute = uia_pattern.Current.FontWeightAttribute.ToString();
                //entity_pattern.ForegroundColorAttribute = uia_pattern.Current.ForegroundColorAttribute.ToString();
                //entity_pattern.HorizontalTextAlignmentAttribute = uia_pattern.Current.HorizontalTextAlignmentAttribute.ToString();
                //entity_pattern.IndentationFirstLineAttribute = uia_pattern.Current.IndentationFirstLineAttribute.ToString();
                //entity_pattern.IndentationLeadingAttribute = uia_pattern.Current.IndentationLeadingAttribute.ToString();
                //entity_pattern.IndentationTrailingAttribute = uia_pattern.Current.IndentationTrailingAttribute.ToString();
                //entity_pattern.IsHiddenAttribute = uia_pattern.Current.IsHiddenAttribute.ToString();
                //entity_pattern.IsItalicAttribute = uia_pattern.Current.IsItalicAttribute.ToString();
                //entity_pattern.IsReadOnlyAttribute = uia_pattern.Current.IsReadOnlyAttribute.ToString();
                //entity_pattern.IsSubscriptAttribute = uia_pattern.Current.IsSubscriptAttribute.ToString();
                //entity_pattern.IsSuperscriptAttribute = uia_pattern.Current.IsSuperscriptAttribute.ToString();
                //entity_pattern.MarginBottomAttribute = uia_pattern.Current.MarginBottomAttribute.ToString();
                //entity_pattern.MarginLeadingAttribute = uia_pattern.Current.MarginLeadingAttribute.ToString();
                //entity_pattern.MarginTopAttribute = uia_pattern.Current.MarginTopAttribute.ToString();
                //entity_pattern.MarginTrailingAttribute = uia_pattern.Current.MarginTrailingAttribute.ToString();
                //entity_pattern.MixedAttributeValue = uia_pattern.Current.MixedAttributeValue.ToString();
                //entity_pattern.OutlineStylesAttribute = uia_pattern.Current.OutlineStylesAttribute.ToString();
                //entity_pattern.OverlineColorAttribute = uia_pattern.Current.OverlineColorAttribute.ToString();
                //entity_pattern.OverlineStyleAttribute = uia_pattern.Current.OverlineStyleAttribute.ToString();
                //entity_pattern.StrikethroughColorAttribute = uia_pattern.Current.StrikethroughColorAttribute.ToString();
                //entity_pattern.StrikethroughStyleAttribute = uia_pattern.Current.StrikethroughStyleAttribute.ToString();
                //entity_pattern.TabsAttribute = uia_pattern.Current.TabsAttribute.ToString();
                //entity_pattern.TextFlowDirectionsAttribute = uia_pattern.Current.TextFlowDirectionsAttribute.ToString();
                //entity_pattern.UnderlineColorAttribute = uia_pattern.Current.UnderlineColorAttribute.ToString();
                //entity_pattern.UnderlineStyleAttribute = uia_pattern.Current.UnderlineStyleAttribute.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsTableItemPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.TableItemPattern entity_pattern = new Sintering.PlatformSpecificInfo.TableItemPattern();
                System.Windows.Automation.TableItemPattern uia_pattern = (System.Windows.Automation.TableItemPattern)element.GetCurrentPattern(TableItemPatternIdentifiers.Pattern);
                entity.DetailedEntity.TableItemPatternNode = entity_pattern;
                //entity_pattern.ColumnHeaderItemsProperty = uia_pattern.Current.ColumnHeaderItemsProperty.ToString();
                //entity_pattern.RowHeaderItemsProperty = uia_pattern.Current.RowHeaderItemsProperty.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsGridItemPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.GridItemPattern entity_pattern = new Sintering.PlatformSpecificInfo.GridItemPattern();
                System.Windows.Automation.GridItemPattern uia_pattern = (System.Windows.Automation.GridItemPattern)element.GetCurrentPattern(GridItemPatternIdentifiers.Pattern);
                entity.DetailedEntity.GridItemPatternNode = entity_pattern;
                entity_pattern.ColumnProperty = uia_pattern.Current.Column.ToString();
                entity_pattern.ColumnSpanProperty = uia_pattern.Current.ColumnSpan.ToString();
                entity_pattern.ContainingGridProperty = uia_pattern.Current.ContainingGrid.ToString();
                entity_pattern.RowProperty = uia_pattern.Current.Row.ToString();
                entity_pattern.RowSpanProperty = uia_pattern.Current.RowSpan.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsTogglePatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.TogglePattern entity_pattern = new Sintering.PlatformSpecificInfo.TogglePattern();
                System.Windows.Automation.TogglePattern uia_pattern = (System.Windows.Automation.TogglePattern)element.GetCurrentPattern(TogglePatternIdentifiers.Pattern);
                entity.DetailedEntity.TogglePatternNode = entity_pattern;
                entity_pattern.ToggleStateProperty = uia_pattern.Current.ToggleState.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsTablePatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.TablePattern entity_pattern = new Sintering.PlatformSpecificInfo.TablePattern();
                System.Windows.Automation.TablePattern uia_pattern = (System.Windows.Automation.TablePattern)element.GetCurrentPattern(TablePatternIdentifiers.Pattern);
                entity.DetailedEntity.TablePatternNode = entity_pattern;
                entity_pattern.ColumnHeadersProperty = string.Join(",", uia_pattern.Current.GetColumnHeaders().Select(x => x.Current.Name).ToArray());
                entity_pattern.RowHeadersProperty = string.Join(",", uia_pattern.Current.GetRowHeaders().Select(x => x.Current.Name).ToArray());
                entity_pattern.RowOrColumnMajorProperty = uia_pattern.Current.RowOrColumnMajor.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsSelectionItemPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.SelectionItemPattern entity_pattern = new Sintering.PlatformSpecificInfo.SelectionItemPattern();
                System.Windows.Automation.SelectionItemPattern uia_pattern = (System.Windows.Automation.SelectionItemPattern)element.GetCurrentPattern(SelectionItemPatternIdentifiers.Pattern);
                entity.DetailedEntity.SelectionItemPatternNode = entity_pattern;
                entity_pattern.IsSelectedProperty = uia_pattern.Current.IsSelected.ToString();
                entity_pattern.SelectionContainerProperty = uia_pattern.Current.SelectionContainer.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsRangeValuePatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.RangeValuePattern entity_pattern = new Sintering.PlatformSpecificInfo.RangeValuePattern();
                System.Windows.Automation.RangeValuePattern uia_pattern = (System.Windows.Automation.RangeValuePattern)element.GetCurrentPattern(RangeValuePatternIdentifiers.Pattern);
                entity.DetailedEntity.RangeValuePatternNode = entity_pattern;
                entity_pattern.IsReadOnlyProperty = uia_pattern.Current.IsReadOnly.ToString();
                entity_pattern.LargeChangeProperty = uia_pattern.Current.LargeChange.ToString();
                entity_pattern.MaximumProperty = uia_pattern.Current.Maximum.ToString();
                entity_pattern.MinimumProperty = uia_pattern.Current.Minimum.ToString();
                entity_pattern.SmallChangeProperty = uia_pattern.Current.SmallChange.ToString();
                entity_pattern.ValueProperty = uia_pattern.Current.Value.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsScrollPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.ScrollPattern entity_pattern = new Sintering.PlatformSpecificInfo.ScrollPattern();
                System.Windows.Automation.ScrollPattern uia_pattern = (System.Windows.Automation.ScrollPattern)element.GetCurrentPattern(ScrollPatternIdentifiers.Pattern);
                entity.DetailedEntity.ScrollPatternNode = entity_pattern;
                entity_pattern.HorizontallyScrollableProperty = uia_pattern.Current.HorizontallyScrollable.ToString();
                entity_pattern.HorizontalScrollPercentProperty = uia_pattern.Current.HorizontalScrollPercent.ToString();
                entity_pattern.HorizontalViewSizeProperty = uia_pattern.Current.HorizontalViewSize.ToString();
                entity_pattern.VerticallyScrollableProperty = uia_pattern.Current.VerticallyScrollable.ToString();
                entity_pattern.VerticalScrollPercentProperty = uia_pattern.Current.VerticalScrollPercent.ToString();
                entity_pattern.VerticalViewSizeProperty = uia_pattern.Current.VerticalViewSize.ToString();
                //entity_pattern.NoScroll = uia_pattern.Current.NoScroll.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsInvokePatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.InvokePattern entity_pattern = new Sintering.PlatformSpecificInfo.InvokePattern();
                System.Windows.Automation.InvokePattern uia_pattern = (System.Windows.Automation.InvokePattern)element.GetCurrentPattern(InvokePatternIdentifiers.Pattern);
                entity.DetailedEntity.InvokePatternNode = entity_pattern;
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsGridPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.GridPattern entity_pattern = new Sintering.PlatformSpecificInfo.GridPattern();
                System.Windows.Automation.GridPattern uia_pattern = (System.Windows.Automation.GridPattern)element.GetCurrentPattern(GridPatternIdentifiers.Pattern);
                entity.DetailedEntity.GridPatternNode = entity_pattern;
                entity_pattern.ColumnCountProperty = uia_pattern.Current.ColumnCount.ToString();
                entity_pattern.RowCountProperty = uia_pattern.Current.RowCount.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsWindowPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.WindowPattern entity_pattern = new Sintering.PlatformSpecificInfo.WindowPattern();
                var topleft = element.Current.BoundingRectangle.TopLeft;
                entity.DetailedEntity.ScreenTop = topleft.X;
                entity.DetailedEntity.ScreenLeft = topleft.Y;

                System.Windows.Automation.WindowPattern uia_pattern = (System.Windows.Automation.WindowPattern)element.GetCurrentPattern(WindowPatternIdentifiers.Pattern);
                entity.DetailedEntity.WindowPatternNode = entity_pattern;
                entity_pattern.CanMaximizeProperty = uia_pattern.Current.CanMaximize.ToString();
                entity_pattern.CanMinimizeProperty = uia_pattern.Current.CanMinimize.ToString();
                entity_pattern.IsModalProperty = uia_pattern.Current.IsModal.ToString();
                entity_pattern.IsTopmostProperty = uia_pattern.Current.IsTopmost.ToString();
                entity_pattern.WindowInteractionStateProperty = uia_pattern.Current.WindowInteractionState.ToString();
                entity_pattern.WindowVisualStateProperty = uia_pattern.Current.WindowVisualState.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            // wait for updated data in IsWindowPatternAvailableProperty
            try
            {
                System.Windows.Rect rect = element.Current.BoundingRectangle;
                rect.X -= entity.DetailedEntity.ScreenTop;
                rect.Y -= entity.DetailedEntity.ScreenLeft;
                entity.DetailedEntity.BoundingRectangleProperty = rect.ToString();

                //Console.WriteLine("topleft " + rect+ " "+Top +" "+Left);
            }
            catch
            {
                entity.DetailedEntity.BoundingRectangleProperty = "";
            }


            if (Boolean.Parse(entity.DetailedEntity.IsMultipleViewPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.MultipleViewPattern entity_pattern = new Sintering.PlatformSpecificInfo.MultipleViewPattern();
                System.Windows.Automation.MultipleViewPattern uia_pattern = (System.Windows.Automation.MultipleViewPattern)element.GetCurrentPattern(MultipleViewPatternIdentifiers.Pattern);
                entity.DetailedEntity.MultipleViewPatternNode = entity_pattern;
                entity_pattern.CurrentViewProperty = uia_pattern.Current.CurrentView.ToString();
                //entity_pattern.SupportedViewsProperty = uia_pattern.Current.SupportedViewsProperty.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsTransformPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.TransformPattern entity_pattern = new Sintering.PlatformSpecificInfo.TransformPattern();
                System.Windows.Automation.TransformPattern uia_pattern = (System.Windows.Automation.TransformPattern)element.GetCurrentPattern(TransformPatternIdentifiers.Pattern);
                entity.DetailedEntity.TransformPatternNode = entity_pattern;
                entity_pattern.CanMoveProperty = uia_pattern.Current.CanMove.ToString();
                entity_pattern.CanResizeProperty = uia_pattern.Current.CanResize.ToString();
                entity_pattern.CanRotateProperty = uia_pattern.Current.CanRotate.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsDockPatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.DockPattern entity_pattern = new Sintering.PlatformSpecificInfo.DockPattern();
                System.Windows.Automation.DockPattern uia_pattern = (System.Windows.Automation.DockPattern)element.GetCurrentPattern(DockPatternIdentifiers.Pattern);
                entity.DetailedEntity.DockPatternNode = entity_pattern;
                entity_pattern.DockPositionProperty = uia_pattern.Current.DockPosition.ToString();
                //entity_pattern.Pattern = uia_pattern.Current.Pattern.ToString();
            }

            if (Boolean.Parse(entity.DetailedEntity.IsLegacyIAccessiblePatternAvailableProperty))
            {
                Sintering.PlatformSpecificInfo.LegacyIAccessiblePattern entity_pattern = new Sintering.PlatformSpecificInfo.LegacyIAccessiblePattern();
                System.Windows.Automation.LegacyIAccessiblePattern uia_pattern = (System.Windows.Automation.LegacyIAccessiblePattern)element.GetCurrentPattern(LegacyIAccessiblePatternIdentifiers.Pattern);
                
                entity_pattern.Name = uia_pattern.Current.Name;
                entity_pattern.Value = uia_pattern.Current.Value;
                entity_pattern.ChildId = uia_pattern.Current.ChildId.ToString();
                entity_pattern.DefaultAction = uia_pattern.Current.DefaultAction;
                entity_pattern.Description = uia_pattern.Current.Description;
                entity_pattern.Help = uia_pattern.Current.Help;
                entity_pattern.KeyboardShortcut = uia_pattern.Current.KeyboardShortcut;
                entity_pattern.Role = uia_pattern.Current.Role.ToString();
                entity_pattern.State = uia_pattern.Current.State.ToString();
                entity_pattern.Pattern = LegacyIAccessiblePatternIdentifiers.Pattern.ProgrammaticName;
                entity.DetailedEntity.LegacyIAccessiblePatternNode = entity_pattern;
            }
            #endregion
        }

    }
}
