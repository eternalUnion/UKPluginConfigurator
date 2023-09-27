using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginConfig.Patches
{
    [HarmonyPatch(typeof(MenuEsc))]
    public class MenuEscPatch
    {
        public static MethodInfo vm_UnityEngine_UI_Selectable_Select = 
            typeof(Selectable)
            .GetMethods()
            .Where(m =>
                m.IsVirtual
                && !m.IsStatic
                && m.Name == "Select")
            .FirstOrDefault();
        public static MethodInfo sm_MenuEscPatch_HandleField = typeof(MenuEscPatch).GetMethod(nameof(HandleField), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        public static void HandleField(Selectable field)
        {
            if (field != null)
                field.Select();
            else
                EventSystem.current.SetSelectedGameObject(null);
        }

        [HarmonyTranspiler]
        [HarmonyPatch("Update")]
        public static IEnumerable<CodeInstruction> FixNullExcpCausedByUncheckedField(IEnumerable<CodeInstruction> inst)
        {
            List<CodeInstruction> code = inst.ToList();

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Callvirt && code[i].OperandIs(vm_UnityEngine_UI_Selectable_Select))
                {
                    code[i].opcode = OpCodes.Call;
                    code[i].operand = sm_MenuEscPatch_HandleField;
                }
            }

            return code.AsEnumerable();
        }
    }
}
