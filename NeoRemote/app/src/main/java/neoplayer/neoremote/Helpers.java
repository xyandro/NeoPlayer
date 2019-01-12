package neoplayer.neoremote;

public class Helpers {
    public static String getAlphaNumeric(String str) {
        String result = "";
        boolean first = true;
        boolean addSpace = false;
        for (char c : str.toCharArray()) {
            if (Character.isLetterOrDigit(c)) {
                if ((!first) && (addSpace))
                    result += " ";
                first = addSpace = false;
                result += c;
            } else
                addSpace = true;
        }
        return result;
    }

    public static int stringCompare(String str1, String str2) {
        return stringCompare(str1, str2, true, true, false);
    }

    public static int stringCompare(String str1, String str2, boolean ascending) {
        return stringCompare(str1, str2, ascending, true, false);
    }

    public static int stringCompare(String str1, String str2, boolean ascending, boolean caseSensitive) {
        return stringCompare(str1, str2, ascending, caseSensitive, false);
    }

    public static int stringCompare(String str1, String str2, boolean ascending, boolean caseSensitive, boolean alphanumeric) {
        if (str1 == null)
            if (str2 == null)
                return 0;
            else
                return 1;
        if (str2 == null)
            return -1;

        if (!caseSensitive) {
            str1 = str1.toLowerCase();
            str2 = str2.toLowerCase();
        }

        if (alphanumeric) {
            str1 = getAlphaNumeric(str1);
            str2 = getAlphaNumeric(str2);
        }

        int index1 = 0;
        int index2 = 0;
        int orderMultiplier = ascending ? 1 : -1;
        while (true) {
            if (index1 >= str1.length())
                if (index2 >= str2.length())
                    return 0;
                else
                    return -orderMultiplier;
            if (index2 >= str2.length())
                return orderMultiplier;

            if ((Character.isDigit(str1.charAt(index1))) && (Character.isDigit(str2.charAt(index2)))) {
                int int1 = 0;
                while ((index1 < str1.length()) && (Character.isDigit(str1.charAt(index1))))
                    int1 = int1 * 10 + str1.charAt(index1++) - '0';
                int int2 = 0;
                while ((index2 < str2.length()) && (Character.isDigit(str2.charAt(index2))))
                    int2 = int2 * 10 + str2.charAt(index2++) - '0';
                if (int1 == int2)
                    continue;
                return (int1 - int2) * orderMultiplier;
            }

            int compare = str1.charAt(index1) - str2.charAt(index2);
            if (compare != 0)
                return compare * orderMultiplier;

            ++index1;
            ++index2;
        }
    }
}