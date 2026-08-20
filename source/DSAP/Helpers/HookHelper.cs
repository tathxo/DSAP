using Archipelago.Core.Util;
using DSAP.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DSAP.Helpers
{
    public class HookHelper
    {
        static public bool pauseHookActive { get; set; } = false; // whether the pause hook is currently active

        // creates a hook at loc_start
        // replaces loc_Start with a jmp to the given new_instructions array of bytes
        // Adds replaced_length bytes of instructions from loc_start before the new_instructions, and a jmp back.
        //  -> This means that the first jmp's code only will be actually processed, because a 2nd inserted jmp would simply jmp to the first one, which returns to the original position
        internal static void AddHook(ulong loc_start, int replaced_length, byte[] new_instructions, bool include_replaced_bytes)
        {
            byte[] replaced_instructions = Memory.ReadByteArray(loc_start, replaced_length);
            ulong replacement_func_start_addr = (ulong)Memory.Allocate(1000, Memory.PAGE_EXECUTE_READWRITE);

            var jmpstub = new byte[]
            {
                0xff, 0x25, 0x00, 0x00, 0x00, 0x00,       //jmp    QWORD PTR [rip+0x0]        # 6 <_main+0x6>
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // target address
                // then the address to jump to (8 bytes)
            };
            Array.Copy(BitConverter.GetBytes(replacement_func_start_addr), 0, jmpstub, 6, 8); // target address

            var return_jmp = new byte[]
            {
                0xff, 0x25, 0x00, 0x00, 0x00, 0x00,          // jmp    QWORD PTR [rip+0x8]
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // jmp's target address
            };

            ulong next_write_pos = replacement_func_start_addr;
            // write the replaced instructions if the bool is on
            if (include_replaced_bytes)
            {
                Memory.WriteByteArray(next_write_pos, replaced_instructions); // write the replaced instructions
                next_write_pos += (ulong)replaced_instructions.Length;
            }

            Memory.WriteByteArray(next_write_pos, new_instructions); // write new instructions into its hook area
            next_write_pos += (ulong)new_instructions.Length;

            Memory.WriteByteArray(next_write_pos, return_jmp); // write the return instruction
            next_write_pos += (ulong)6; // point to return address
            Memory.WriteByteArray(next_write_pos, BitConverter.GetBytes(loc_start + (ulong)replaced_length)); // write the return address

            //Memory.WriteByteArray(replacement_func_start_addr + new_instructions.Length, return_jmp);
            Memory.WriteByteArray(loc_start, jmpstub); // write jmp stub (e.g. "create hook")
        }

        internal static void InitPauseHook()
        {
            if (!SaveLoadHelper.SettingsLoaded)
                App.ControlsContext.PauseInGestureMenu = true; // default to true, will also run the hook if it hasn't yet
            else
                MakePauseHook(App.ControlsContext.PauseInGestureMenu);
        }
        internal static void MakePauseHook(bool pauseSettingOn)
        {
            if (App.dsrClient == null) // no client = nothing to hook
                return;

            Log.Logger.Debug($"initing pause hook to {pauseSettingOn}");
            ulong hook1_loc = 0x14024f88a;
            int hook1_length = 17;
            //int hook1_length = 0x26;

            var restoreBytes = new byte[]
            {
            0x44, 0x38, 0xbe, 0xa1, 0x00, 0x00, 0x00,       // CMP        byte ptr [RSI + 0xa1],R15B
            0x0f, 0x94, 0xc3,                               // SETZ       BL
            0x44, 0x38, 0xbe, 0x90, 0x00, 0x00, 0x00        // CMP        byte ptr [RSI + 0x90],R15B 
            };

            // build hook1
            var pause_on_start_menu = new byte[]
            {
            // load MenuMan, check if the relevant byte is set for player to be "in menu". If so, turn on the "pause bytes" in the MoveMapStep
                // push rax
                // movabs rax,[0x141c88d98] // MenuMan
                // add rax,0x50
                // cmp byte ptr [rax],0x01 // needed for going between menus
                // je dowrite
                // cmp byte ptr [rax],0x02
                // je dowrite
                // cmp byte ptr [rax],0x04 // needed for going between menus
                // je dowrite
                // cmp byte ptr [rax],0x05
                // je dowrite
                // cmp byte ptr [rax],0x08
                // je dowrite
                // jmp unwrite
                // dowrite: 
                // mov eax,0x0101
                // mov word ptr [rsi+0x90],ax
                // jmp done
                // unwrite:
                // mov eax,0x0000
                // mov WORD PTR [rsi+0x90],ax
                // done:
                // pop rax
                0x50,                                     // push   rax
                0x48, 0xa1, 0x98, 0x8d, 0xc8, 0x41, 0x01, // movabs rax,ds:0x141c88d98
                0x00, 0x00, 0x00,
                0x48, 0x83, 0xc0, 0x50,                   // add    rax,0x50
                0x80, 0x38, 0x01,                         // cmp    BYTE PTR [rax],0x1
                0x74, 0x16,                               // je     2a <dowrite>
                0x80, 0x38, 0x02,                         // cmp    BYTE PTR [rax],0x2
                0x74, 0x11,                               // je     2a <dowrite>
                0x80, 0x38, 0x04,                         // cmp    BYTE PTR [rax],0x4
                0x74, 0x0c,                               // je     2a <dowrite>
                0x80, 0x38, 0x05,                         // cmp    BYTE PTR [rax],0x5
                0x74, 0x07,                               // je     2a <dowrite>
                0x80, 0x38, 0x08,                         // cmp    BYTE PTR [rax],0x8
                0x74, 0x02,                               // je     2a <dowrite>
                0xeb, 0x0e,                               // jmp    38 <unwrite>
                //// dowrite:
                0xb8, 0x01, 0x01, 0x00, 0x00,               // mov eax,0x101
                0x66, 0x89, 0x86, 0x90, 0x00, 0x00, 0x00,   // mov WORD PTR [rsi+0x90],ax
                0xeb, 0x0c,                                 // jmp    44 < done >
                //// unwrite:
                0xb8, 0x00, 0x00, 0x00, 0x00,               // mov eax,0x000
                0x66, 0x89, 0x86, 0x90, 0x00, 0x00, 0x00,   // mov WORD PTR [rsi+0x90],ax
                //// done:
                0x58,                                       // pop rax
                // then add the code we overwrote which checks conditions
                0x44, 0x38, 0xbe, 0xa1, 0x00, 0x00, 0x00,       // CMP        byte ptr [RSI + 0xa1],R15B
                0x0f, 0x94, 0xc3,                               // SETZ       BL
                0x44, 0x38, 0xbe, 0x90, 0x00, 0x00, 0x00        // CMP        byte ptr [RSI + 0x90],R15B 
            };

            // build hook1
            var pause_on_gesture_menu = new byte[]
            {
            // load MenuMan, check if the relevant byte is set for player to be "in gesture menu". If so, turn on the "pause bytes" in the MoveMapStep
                // push rax
                // movabs rax,[0x141c88d98] // MenuMan
                // add rax,0x100
                // cmp byte ptr [rax],0x01 // main menu
                // je dowrite
                // cmp byte ptr [rax],0x02 // switch/sub menu
                // je dowrite
                // cmp byte ptr [rax],0x03 // switch/sub menu
                // je dowrite
                // jmp unwrite
                // dowrite: 
                // mov eax,0x0101
                // mov word ptr [rsi+0x90],ax
                // jmp done
                // unwrite:
                // mov eax,0x0000
                // mov WORD PTR [rsi+0x90],ax
                // done:
                // pop rax
                0x50,                                     // push   rax
                0x48, 0xa1, 0x98, 0x8d, 0xc8, 0x41, 0x01, // movabs rax,ds:0x141c88d98
                0x00, 0x00, 0x00,
                0x48, 0x05, 0x00, 0x01, 0x00, 0x00,       // add    rax,0x100
                0x80, 0x38, 0x01,                         // cmp    BYTE PTR [rax],0x1
                0x74, 0x0c,                               // je     22 <dowrite>
                0x80, 0x38, 0x02,                         // cmp    BYTE PTR [rax],0x2
                0x74, 0x07,                               // je     22 <dowrite>
                0x80, 0x38, 0x03,                         // cmp    BYTE PTR [rax],0x4
                0x74, 0x02,                               // je     22 <dowrite>
                0xeb, 0x0e,                               // jmp    30 <unwrite>
                //// dowrite:
                0xb8, 0x01, 0x01, 0x00, 0x00,               // mov eax,0x101
                0x66, 0x89, 0x86, 0x90, 0x00, 0x00, 0x00,   // mov WORD PTR [rsi+0x90],ax
                0xeb, 0x0c,                                 // jmp    3c < done >
                //// unwrite:
                0xb8, 0x00, 0x00, 0x00, 0x00,               // mov eax,0x000
                0x66, 0x89, 0x86, 0x90, 0x00, 0x00, 0x00,   // mov WORD PTR [rsi+0x90],ax
                //// done:
                0x58,                                       // pop rax
                // then add the code we overwrote which checks conditions
                0x44, 0x38, 0xbe, 0xa1, 0x00, 0x00, 0x00,       // CMP        byte ptr [RSI + 0xa1],R15B
                0x0f, 0x94, 0xc3,                               // SETZ       BL
                0x44, 0x38, 0xbe, 0x90, 0x00, 0x00, 0x00        // CMP        byte ptr [RSI + 0x90],R15B 
            };

            if (pauseSettingOn && !pauseHookActive)
            {
                AddHook(hook1_loc, hook1_length, pause_on_gesture_menu, false);
                pauseHookActive = true;
            }
            else if (!pauseSettingOn && pauseHookActive)
            {
                Memory.WriteByteArray(hook1_loc, restoreBytes);
                pauseHookActive = false;
            }
        }
        // hook to prevent items going into inventory
        internal static void AddAPItemHook(long min, long max)
        {
            ulong target_func_start = 0x1407479E0;
            byte[] replaced_instructions = Memory.ReadByteArray(target_func_start, 14);

            //CMP r9d,0x12345678
            //JL OVER
            //CMP r9d,0x12345678
            //JG OVER
            // RET and 5 nops (could be replaced with mov r9d,<value>)
            // OVER (label)
            // 14 nops (replaced with source 14 bytes overwritten by jmp instruction)
            //  jmp        qword[rip+0]
            // <return address>
            var new_instructions = new byte[]
            {
                0x41, 0x81, 0xf8, 0x78, 0x56, 0x34, 0x12,    // cmp r9d,0x12345678
                0x7c, 0x0f,                                  // jl     OVER
                0x41, 0x81, 0xf8, 0x78, 0x56, 0x34, 0x12,    // cmp    r9d,0x12345678
                0x7f, 0x06,                                  // jg     OVER
                0xc3, 0x90, 0x90, 0x90, 0x90, 0x90,          // ret and 5 nops
                //0x41, 0xb8, 0x72, 0x01, 0x00, 0x00,          // mov    r9d,0x172 (dec 370)
                // OVER (label)
                0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90,    // 14 nops -> get replaced with source 14 bytes
                0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90,
            };

            Array.Copy(BitConverter.GetBytes(min), 0, new_instructions, 3, 4); // min
            Array.Copy(BitConverter.GetBytes(max), 0, new_instructions, 12, 4); // max
            Array.Copy(replaced_instructions, 0, new_instructions, 24, 14); // replaced_instructions

            AddHook(target_func_start, 14, new_instructions, false);
        }
        // hook to prevent item popups for "location" items...we popup these explicitly in the LocationCompleted instead
        internal static void AddAPItemPopupHook(long min, long max)
        {
            ulong target_func_start = 0x140728c90;
            byte[] replaced_instructions = Memory.ReadByteArray(target_func_start, 14);

            //CMP r9d,0x12345678
            //JL OVER
            //CMP r9d,0x12345678
            //JG OVER
            // RET and 5 nops (could be replaced with mov r9d,<value>)
            // OVER (label)
            // 14 nops (replaced with source 14 bytes overwritten by jmp instruction)
            //  jmp        qword[rip+0]
            // <return address>
            var new_instructions = new byte[]
            {
                0x41, 0x81, 0xf8, 0x78, 0x56, 0x34, 0x12,    // cmp r9d,0x12345678
                0x7c, 0x0f,                                  // jl     OVER
                0x41, 0x81, 0xf8, 0x78, 0x56, 0x34, 0x12,    // cmp    r9d,0x12345678
                0x7f, 0x06,                                  // jg     OVER
                0xc3, 0x90, 0x90, 0x90, 0x90, 0x90,          // ret and 5 nops
                //0x41, 0xb8, 0x72, 0x01, 0x00, 0x00,          // mov    r9d,0x172 (dec 370)
                // OVER (label)
                0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90,    // 14 nops -> get replaced with source 14 bytes
                0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90,
            };

            Array.Copy(BitConverter.GetBytes(min), 0, new_instructions, 3, 4); // min
            Array.Copy(BitConverter.GetBytes(max), 0, new_instructions, 12, 4); // max
            Array.Copy(replaced_instructions, 0, new_instructions, 24, 14); // replaced_instructions

            AddHook(target_func_start, 14, new_instructions, false);
        }
    }
}
