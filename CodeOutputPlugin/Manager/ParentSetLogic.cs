﻿using CodeOutputPlugin.Models;
using Gum;
using Gum.DataTypes;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace CodeOutputPlugin.Manager
{
    public static class ParentSetLogic
    {
        public static void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue, CodeOutputProjectSettings codeOutputProjectSettings)
        {
            ///////////////////////Early Out//////////////////
            if(variableName != "Parent" || instance == null || codeOutputProjectSettings.IsCodeGenPluginEnabled == false)
            {
                return;
            }
            /////////////////////End Early Out////////////////

            var currentState = SelectedState.Self.SelectedStateSave;
            var rfv = new RecursiveVariableFinder(currentState);

            var newParentName = rfv.GetValue<string>($"{instance.Name}.Parent");
            InstanceSave newParent = null;
            if (!string.IsNullOrEmpty(newParentName))
            {
                newParent = element.GetInstance(newParentName);
            }

            var response = CanInstanceRemainAsAChildOf(instance, newParent, element);


            if (!response.Succeeded)
            {
                currentState.SetValue($"{instance.Name}.Parent", oldValue, "string");

                // Maybe an output message is not obvious enough?
                //GumCommands.Self.GuiCommands.PrintOutput(childResponse.Message);
                GumCommands.Self.GuiCommands.ShowMessage(response.Message);
            }
        }

        internal static void HandleNewCreatedInstance(ElementSave element, InstanceSave instance,  CodeOutputProjectSettings codeOutputProjectSettings)
        {
            ///////////////////////Early Out//////////////////
            if (codeOutputProjectSettings.IsCodeGenPluginEnabled == false)
            {
                return;
            }
            /////////////////////End Early Out////////////////
            
            var rfv = new RecursiveVariableFinder(element.DefaultState);
            var newParentName = rfv.GetValue<string>($"{instance.Name}.Parent");

            InstanceSave newParent = null;
            if (!string.IsNullOrEmpty(newParentName))
            {
                newParent = element.GetInstance(newParentName);
            }

            var childResponse = CanInstanceRemainAsAChildOf(instance, newParent, element);

            if(!childResponse.Succeeded)
            {
                element.Instances.Remove(instance);

                GumCommands.Self.GuiCommands.ShowMessage(childResponse.Message);

            }
        }

        static int CountInstancesWithParent(ElementSave element, string name)
        {
            int count = 0;
            var defaultVariables = element.DefaultState.Variables;

            foreach(var variable in defaultVariables)
            {
                var isParent = variable.GetRootName() == "Parent";

                if(isParent && variable.SourceObject != null && (variable.Value as string) == name)
                {
                    count++;
                }
            }
            return count;
        }

        private static GeneralResponse CanInstanceRemainAsAChildOf(InstanceSave instance, InstanceSave newParent, ElementSave element)
        {
            var toReturn = CanInstanceBeChildBasedOnXamarinFormsSkiaRestrictions(instance, newParent, element);

            if(toReturn.Succeeded)
            {
                // even if it's okay, it could be that the parent only supports Contents and doesn't have .Children.
                // In that case, we should only allow 1 child:
                var parentType = newParent?.BaseType ?? element.BaseType;

                var hasContent = CodeGenerator.DoesTypeHaveContent(parentType);

                if (hasContent)
                {
                    var childrenCount = 
                        CountInstancesWithParent(element, newParent?.Name);

                    if (childrenCount > 1)
                    {
                        var parentName = newParent?.Name ?? element.Name;
                        var message =
                            $"{instance.Name} cannot be added as a child to {parentName} because {parentName} is a Xamarin Forms object which has Content, so it can only have 1 child";
                        toReturn = GeneralResponse.UnsuccessfulWith(message);
                    }
                }
            }

            return toReturn;
        }

        private static GeneralResponse CanInstanceBeChildBasedOnXamarinFormsSkiaRestrictions(InstanceSave instance, InstanceSave newParent, ElementSave element)
        {
            VisualApi parentVisualApi;
            VisualApi childVisualApi = CodeGenerator.GetVisualApiForInstance(instance, element);
            if (newParent != null)
            {
                parentVisualApi = CodeGenerator.GetVisualApiForInstance(newParent, element);
            }
            else
            {
                parentVisualApi = CodeGenerator.GetVisualApiForElement(element);
            }

            var parentType = newParent?.BaseType ?? element.BaseType;
            var isParentSkiaCanvas = parentType?.EndsWith("/SkiaGumCanvasView") == true;

            var childName = instance.Name;
            var parentName = newParent?.Name ?? element.Name;

            if (parentVisualApi == childVisualApi)
            {
                if (isParentSkiaCanvas && childVisualApi == VisualApi.XamarinForms)
                {
                    return GeneralResponse.UnsuccessfulWith(
                        $"Can't add {childName} to parent {parentName} because the parent is a a SkiaGumCanvasView which can only contain non-XamarinForms objects");
                }
                else
                {
                    // all good!
                    return GeneralResponse.SuccessfulResponse;
                }
            }
            else
            {

                // they don't match, but we can have a special case where children can be added to a parent that is a SkiaGumCanvasView
                if (childVisualApi == VisualApi.Gum && isParentSkiaCanvas)
                {
                    // Gum child added to parent skia canvas, so that's okay:
                    return GeneralResponse.SuccessfulResponse;
                }
                else
                {

                    // they don't match, and it's not a Gum object in skia canvas:
                    var message = childVisualApi == VisualApi.Gum
                        ? $"Can't add {childName} to parent {parentName} because the parent needs to either be a SkiaGumCanvasView, or contained in a SkiaGumCanvasView"
                        : $"Can't add {childName} to parent {parentName} because the parent is in a Skia canvas and the child is a Xamarin Forms object.";
                    return GeneralResponse.UnsuccessfulWith(message);
                }
            }
        }

    }
}