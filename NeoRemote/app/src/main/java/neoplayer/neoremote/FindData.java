package neoplayer.neoremote;

public class FindData {
    static public class FindType {
        public String name;
        public int paramCount;

        public FindType(String name, int paramCount) {
            this.name = name;
            this.paramCount = paramCount;
        }

        @Override
        public String toString() {
            return name;
        }
    }

    static FindType None = new FindType("None", 0);
    static FindType Empty = new FindType("Empty", 0);
    static FindType NonEmpty = new FindType("Non-Empty", 0);
    static FindType Equal = new FindType("==", 1);
    static FindType NotEqual = new FindType("<>", 1);
    static FindType Contains = new FindType("Contains", 1);
    static FindType NotContains = new FindType("Not Contains", 1);
    static FindType Greater = new FindType(">", 1);
    static FindType GreaterOrEqual = new FindType(">=", 1);
    static FindType Less = new FindType("<", 1);
    static FindType LessOrEqual = new FindType("<=", 1);
    static FindType Between = new FindType("Between", 2);
    static FindType NotBetween = new FindType("Not Between", 2);
    static FindType[] findTypes = new FindType[]{None, Empty, NonEmpty, Equal, NotEqual, Contains, NotContains, Greater, GreaterOrEqual, Less, LessOrEqual, Between, NotBetween};

    public String tag;
    public FindType findType = None;
    public String value1 = null;
    public String value2 = null;

    public FindData(String tag) {
        this.tag = tag;
    }

    public FindData copy() {
        FindData findData = new FindData(tag);
        findData.findType = findType;
        findData.value1 = value1;
        findData.value2 = value2;
        return findData;
    }

    public boolean matches(VideoFile videoFile) {
        if (findType == None)
            return true;

        String value = videoFile.tags.get(tag);
        if (value == null)
            return findType == Empty;

        value = value.toLowerCase();

        if (findType == NonEmpty)
            return true;
        if (findType == Equal)
            return Helpers.stringCompare(value, value1) == 0;
        if (findType == NotEqual)
            return Helpers.stringCompare(value, value1) != 0;
        if (findType == Contains)
            return value.contains(value1);
        if (findType == NotContains)
            return !value.contains(value1);
        if (findType == Greater)
            return Helpers.stringCompare(value, value1) > 0;
        if (findType == GreaterOrEqual)
            return Helpers.stringCompare(value, value1) >= 0;
        if (findType == Less)
            return Helpers.stringCompare(value, value1) < 0;
        if (findType == LessOrEqual)
            return Helpers.stringCompare(value, value1) <= 0;
        if (findType == Between)
            return (Helpers.stringCompare(value, value1) >= 0) && (Helpers.stringCompare(value, value2) <= 0);
        if (findType == NotBetween)
            return (Helpers.stringCompare(value, value1) < 0) || (Helpers.stringCompare(value, value2) > 0);

        return false;
    }
}
