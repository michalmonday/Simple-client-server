/*
    CE303 Client-server system
    Student ID: 1904535

    In c# some things can be done easily by using a single line of code.
    In java the same things can't be accomplished as concisely,
    so this class will provide 1-liner solutions to such problems, that
    may not be related with each other, hence the general "JavaFix" name
*/

package main;

import javax.swing.*;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.List;

public class JavaFix {
    static public boolean isServerAlreadyRunning() {
        boolean csharp_runs = processPidsByName("Server").size() > 1;
        boolean java_runs = processPidsByName("ServerForm").size() > 1;
        return csharp_runs|| java_runs;
    }

    static public List<Integer> processPidsByName(String process_name) {
        List<Integer> server_pids = new ArrayList<>();

        // Finding C# server processes
        List<String> tasklist_find_response = executeCommand("tasklist | find \"" + process_name + "\"");
        for (String line : tasklist_find_response) {
            String[] tokens = line.split("\\s+");
            if (tokens.length > 1) {
                String pid_str = tokens[1];
                server_pids.add(Integer.parseInt(pid_str));
            }
        }

        // Finding Java server processes
        List<String> jps_find_response = executeCommand("jps | find \"ServerForm\"");
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
            System.out.println("JavaFix.executeCommand exception: " + e.getMessage());
        }
        return response_lines;
    }


    // Class "NoSelectionModel was copied from: https://stackoverflow.com/questions/31669350/disable-jlist-cell-selection-property
    // Author: morpheus05
    public static class NoSelectionModel extends DefaultListSelectionModel {
        @Override
        public void setAnchorSelectionIndex(final int anchorIndex) {}
        @Override
        public void setLeadAnchorNotificationEnabled(final boolean flag) {}
        @Override
        public void setLeadSelectionIndex(final int leadIndex) {}
        @Override
        public void setSelectionInterval(final int index0, final int index1) { }
    }
}
