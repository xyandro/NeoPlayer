package neoplayer.neoremote;

import java.util.ArrayList;
import java.util.Collection;
import java.util.Collections;
import java.util.Comparator;

public class SortData {
    public enum SortDirection {None, Ascending, Descending}

    public class SortItem {
        public SortDirection direction;
        public Integer priority;
        public String tag;

        public SortItem(SortDirection direction, Integer priority, String tag) {
            this.direction = direction;
            this.priority = priority;
            this.tag = tag;
        }
    }

    final private ArrayList<SortItem> sortItems = new ArrayList<>();

    private SortData() {
    }

    public SortData(Collection<String> tags) {
        for (String tag : tags)
            sortItems.add(new SortItem(SortDirection.None, null, tag));
        Collections.sort(sortItems, new Comparator<SortItem>() {
            @Override
            public int compare(SortItem sortItem1, SortItem sortItem2) {
                return sortItem1.tag.compareTo(sortItem2.tag);
            }
        });
    }

    public int size() {
        return sortItems.size();
    }

    public SortItem getAlphaOrder(int index) {
        return sortItems.get(index);
    }

    public SortItem getPriorityOrder(int index) {
        for (SortItem sortItem : sortItems)
            if ((sortItem.priority != null) && (sortItem.priority == index))
                return sortItem;
        return null;
    }

    private void updatePriorities() {
        ArrayList<SortItem> prioritySortItems = new ArrayList<>();
        for (SortItem sortItem : sortItems) {
            if (sortItem.direction == SortDirection.None)
                sortItem.priority = null;
            else {
                if (sortItem.priority == null)
                    sortItem.priority = sortItems.size();
                prioritySortItems.add(sortItem);
            }
        }

        Collections.sort(prioritySortItems, new Comparator<SortItem>() {
            @Override
            public int compare(SortItem item1, SortItem item2) {
                return item1.priority - item2.priority;
            }
        });

        int priority = 0;
        for (SortItem sortItem : prioritySortItems)
            sortItem.priority = ++priority;
    }

    public void setSortDirection(SortItem sortItem, SortDirection sortDirection) {
        sortItem.direction = sortDirection;
        updatePriorities();
    }

    public void toggle(SortItem sortItem, boolean disable) {
        if (disable)
            sortItem.direction = SortDirection.None;
        else if (sortItem.direction == SortDirection.Ascending)
            sortItem.direction = SortDirection.Descending;
        else
            sortItem.direction = SortDirection.Ascending;
        updatePriorities();
    }

    public void clear() {
        for (SortItem sortItem : sortItems) {
            sortItem.direction = SortDirection.None;
            sortItem.priority = null;
        }
    }

    public SortData copy() {
        SortData sortData = new SortData();
        for (SortItem sortItem : sortItems)
            sortData.sortItems.add(new SortItem(sortItem.direction, sortItem.priority, sortItem.tag));
        return sortData;
    }
}
