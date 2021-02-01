/*
    CE303 Client-server system
    Student ID: 1904535

    In c# some things can be done easily by using a single line of code.
    In java the same things can't be accomplished as concisely,
    so this class will provide 1-liner solutions to such problems, that
    may not be related with each other, hence the general "JavaFix" name
*/

package main;

import com.sun.jna.Native;
import com.sun.jna.win32.StdCallLibrary;

import javax.swing.*;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.List;

public class JavaFix {
    static public boolean isServerRunning() {
        return processPidsByName(new String[]{ "Server", "ServerForm" }).size() > 0;
    }

    static public List<Integer> processPidsByName(String ... process_names) {
        List<Integer> server_pids = new ArrayList<>();

        for (String process_name : process_names) {
            // Finding C# server processes
            List<String> tasklist_find_response = executeCommand("tasklist | find \"" + process_name + "\"");
            // Example output:
            //      C:\Users\michal>tasklist | find "Server"
            //      Server.exe                    1224 Console                    1     22,200 K

            for (String line : tasklist_find_response) {
                String[] tokens = line.split("\\s+");
                if (tokens.length > 1) {
                    String pid_str = tokens[1];
                    server_pids.add(Integer.parseInt(pid_str));
                }
            }

            // Finding Java server processes
            List<String> jps_find_response = executeCommand("jps | find \"" + process_name + "\"");
            // Example output:
            //      C:\Users\michal>jps | find "Server"
            //      17780 ServerForm

            for (String line : jps_find_response) {
                String[] tokens = line.split("\\s+");
                if (tokens.length > 1) {
                    String pid_str = tokens[0];
                    server_pids.add(Integer.parseInt(pid_str));
                }
            }
        }
        return server_pids;
    }

    public static List<String> executeCommand(String cmd) {
        // in return of many commands newline is used as separator of values,
        // so that's why a list of strings is returned

        List<String> response_lines = new ArrayList<>();
        try {
            Process p = Runtime.getRuntime().exec("cmd.exe /C " + cmd);
            BufferedReader input = new BufferedReader(new InputStreamReader(p.getInputStream()));
            String line;
            while ((line = input.readLine()) != null)
                response_lines.add(line);
            input.close();
        } catch (IOException e) {

        }
        return response_lines;
    }

    public static void MessageBox_show(String msg) {
        JOptionPane.showMessageDialog(null, msg, "Client info", JOptionPane.INFORMATION_MESSAGE);
    }

    public static int CreateMutex(String name) {
        return Kernel32.SYNC_INSTANCE.CreateMutexA(0, false, name);
    }

    public static int waitOne(int mutex_handle, int milliseconds) {
        return Kernel32.SYNC_INSTANCE.WaitForSingleObject(mutex_handle, 0);
    }

    public static boolean ReleaseMutex(int mutex_handle) {
        boolean result = Kernel32.SYNC_INSTANCE.ReleaseMutex(mutex_handle);
        if (result == false)
            System.out.printf("ReleaseMutex failed, GetLastError = %d\n", Kernel32.SYNC_INSTANCE.GetLastError());
        return result;
    }

    // StdCallLibrary is provided by JNA (Java Native Access), it can be used
    // to call WinApi functions that allow using system wide mutex on Windows.
    // It requires "jna-5.6.0.jar" file, I did the following to set it up:
    //      1. I put the "jna-5.6.0.jar" in Client project directory.
    //      2. Right click on it in "Project" file explorer on the left of Intellij and clicked "add as library"
    //      3. Right click on "Client" in file explorer and clicked "Open module settings"
    //      4. Went to "dependencies" added "+" sign, and added "jna-5.6.0.jar"
    //              (it was already there anyway but this time I selected "Runtime" instead of "Compile")
    //              (so in the end, there were 2 entries in total for it, 1 with "compile" and 1 with "runtime" selected)
    //
    // https://github.com/java-native-access/jna
    // Download link for jna-5.6.0.jar: https://repo1.maven.org/maven2/net/java/dev/jna/jna/5.6.0/jna-5.6.0.jar
    public interface Kernel32 extends StdCallLibrary {
        Kernel32 INSTANCE = (Kernel32) Native.load("kernel32", Kernel32.class);
        Kernel32 SYNC_INSTANCE = (Kernel32) Native.synchronizedLibrary(INSTANCE);

        // https://docs.microsoft.com/en-us/windows/win32/sync/using-mutex-objects

        // "A" in CreateMutexA stands for Ansi
        // https://docs.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-createmutexa
        int CreateMutexA(int mutex_attributes, boolean is_initial_owner, String name);
        /*   Docs: The name can have a "Global" or "Local" prefix to explicitly create the object
             in the global or session namespace. The remainder of the name can contain any
             character except the backslash character (). For more information, see Kernel
             Object Namespaces. Fast user switching is implemented using Terminal Services
             sessions. Kernel object names must follow the guidelines outlined for Terminal
             Services so that applications can support multiple users.

             Me: I checked it and it worked well with other processes even
             without "Global" prefix.  */

        // https://docs.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-releasemutex
        boolean ReleaseMutex(int mutex_handle);

        // https://docs.microsoft.com/en-us/windows/win32/api/errhandlingapi/nf-errhandlingapi-getlasterror
        int GetLastError();

        // https://docs.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-waitforsingleobject
        int WaitForSingleObject(int mutex_handle, int millis);
        /*  WAIT_ABANDONED - 0x00000080L
                The specified object is a mutex object that was not released by the thread that owned the mutex object before the owning thread terminated. Ownership of the mutex object is granted to the calling thread and the mutex state is set to nonsignaled.
                If the mutex was protecting persistent state information, you should check it for consistency.
            WAIT_OBJECT_0 - 0x00000000L
                The state of the specified object is signaled.
            WAIT_TIMEOUT - 0x00000102L
                The time-out interval elapsed, and the object's state is nonsignaled.
            WAIT_FAILED - (DWORD)0xFFFFFFFF
                The function has failed. To get extended error information, call GetLastError.  */
    }

    public static void killServer() {
        List<Integer> pids = processPidsByName("Server");
        pids.addAll(processPidsByName("ServerForm"));

        for (int pid : pids) {
            List<String> output = executeCommand("taskkill /F /PID " + Integer.toString(pid));
            for (String line : output)
                System.out.println(line);
        }
    }
}
