package neoplayer.neoremote;

import java.util.ArrayList;
import java.util.Collection;
import java.util.Collections;
import java.util.Comparator;

public class SortData {
    public enum SortDirection {None, Ascending, Descending}

    public class SortItem {
        public String tag;
        public SortDirection direction;

        public SortItem(String tag, SortDirection direction) {
            this.tag = tag;
            this.direction = direction;
        }
    }

    final public ArrayList<SortItem> sortItems = new ArrayList<>();

    private SortData() {
    }

    public SortData(Collection<String> tags) {
        for (String tag : tags)
            sortItems.add(new SortItem(tag, SortDirection.None));
    }

    public void toggle(SortItem sortItem, boolean disable) {
        if (disable)
            sortItem.direction = SortDirection.None;
        else if (sortItem.direction == SortDirection.Ascending)
            sortItem.direction = SortDirection.Descending;
        else
            sortItem.direction = SortDirection.Ascending;

        Collections.sort(sortItems, new Comparator<SortItem>() {
            @Override
            public int compare(SortItem item1, SortItem item2) {
                if ((item1.direction != SortDirection.None) && (item2.direction == SortDirection.None))
                    return -1;
                if ((item1.direction == SortDirection.None) && (item2.direction != SortDirection.None))
                    return 1;
                if (item1.direction != SortDirection.None)
                    return 0;
                return item1.tag.compareTo(item2.tag);
            }
        });
    }

    public SortData Copy() {
        SortData sortData = new SortData();
        for (SortItem sortItem : sortItems)
            sortData.sortItems.add(new SortItem(sortItem.tag, sortItem.direction));
        return sortData;
    }
}
