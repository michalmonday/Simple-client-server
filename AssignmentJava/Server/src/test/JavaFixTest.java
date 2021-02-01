package test;

import main.JavaFix;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

class JavaFixTest {

    @BeforeEach
    void setUp() {
    }

    @AfterEach
    void tearDown() {
    }

    @Test
    void processPidsByName() {
        List<Integer> pids = JavaFix.processPidsByName("explorer.exe");
        assertTrue(pids.size() > 0);
    }

    @Test
    void executeCommand() {
        String command = "echo hello world";
        List<String> output_lines = JavaFix.executeCommand(command);
        String first_line_of_output = output_lines.get(0);
        assertTrue(first_line_of_output.equals("hello world"), "Output of 'echo' command is incorrect.");
        assertEquals(1, output_lines.size(), "Output contains incorrect number of lines, should be only 1.");
    }

    /*@Test
    void isServerAlreadyRunning() {
        // isServerAlreadyRunning is supposed to be called by the server
        // It checks if the total number of Server processes is above 1
        // If that's the case, the program is terminated.
        // So testing this method isn't possible with the current design.
        // And I don't want to change the design just for the sake of increasing
        // test coverage.

        assertFalse(JavaFix.isServerAlreadyRunning());
        // This can't be used to start the server because then the process isn't called "Server"
        //Thread t = new Thread(()-> { ServerForm server_form = new ServerForm(); });
        //t.setDaemon(true);
        //t.start();

        // So instead I could use the same server starting method that is used
        // by the client

        String command =  "\"C:\\Program Files\\Java\\jdk-11.0.6\\bin\\java.exe\" " +
                "\"-javaagent:C:\\Program Files\\JetBrains\\IntelliJ IDEA 2019.3.3\\lib\\idea_rt.jar=55450:" +
                "C:\\Program Files\\JetBrains\\IntelliJ IDEA 2019.3.3\\bin\" " +
                "-Dfile.encoding=UTF-8 " +
                "-classpath \"C:\\Users\\michal\\Desktop\\coursework\\3\\ce303 adv_prog\\Assignment\\AssignmentJava\\Server\\out\\production\\Server;C:\\Users\\michal\\.m2\\repository\\org\\junit\\jupiter\\junit-jupiter\\5.4.2\\junit-jupiter-5.4.2.jar;C:\\Users\\michal\\.m2\\repository\\org\\junit\\jupiter\\junit-jupiter-api\\5.4.2\\junit-jupiter-api-5.4.2.jar;C:\\Users\\michal\\.m2\\repository\\org\\apiguardian\\apiguardian-api\\1.0.0\\apiguardian-api-1.0.0.jar;C:\\Users\\michal\\.m2\\repository\\org\\opentest4j\\opentest4j\\1.1.1\\opentest4j-1.1.1.jar;C:\\Users\\michal\\.m2\\repository\\org\\junit\\platform\\junit-platform-commons\\1.4.2\\junit-platform-commons-1.4.2.jar;C:\\Users\\michal\\.m2\\repository\\org\\junit\\jupiter\\junit-jupiter-params\\5.4.2\\junit-jupiter-params-5.4.2.jar;C:\\Users\\michal\\.m2\\repository\\org\\junit\\jupiter\\junit-jupiter-engine\\5.4.2\\junit-jupiter-engine-5.4.2.jar;" +
                "C:\\Users\\michal\\.m2\\repository\\org\\junit\\platform\\junit-platform-engine\\1.4.2\\junit-platform-engine-1.4.2.jar\" " +
                "main.ServerForm";

        try {
            Runtime.getRuntime().exec(command);
            try { Thread.sleep(5000); } catch (InterruptedException e) {}
        } catch (IOException e) {
            e.printStackTrace();
        }
        assertTrue(JavaFix.isServerAlreadyRunning());
    }*/
}