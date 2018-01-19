package neoplayer.neoremote;

import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.Button;
import android.widget.TextView;

public class SortAdapter extends BaseAdapter {
    private final MainActivity mainActivity;
    private final SortData sortData;

    public SortAdapter(MainActivity mainActivity, SortData sortData) {
        super();
        this.mainActivity = mainActivity;
        this.sortData = sortData;
    }

    @Override
    public int getCount() {
        return sortData.sortItems.size();
    }

    @Override
    public Object getItem(int i) {
        return sortData.sortItems.get(i);
    }

    @Override
    public long getItemId(int i) {
        return i;
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        final SortData.SortItem sortItem = (SortData.SortItem) getItem(position);

        View view = mainActivity.getLayoutInflater().inflate(R.layout.fragment_sort_listitem, parent, false);

        ((TextView) view.findViewById(R.id.name)).setText(sortItem.tag);
        ((Button) view.findViewById(R.id.value)).setText(sortItem.direction.toString());
        view.findViewById(R.id.value).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                sortData.toggle(sortItem, false);
                notifyDataSetChanged();
            }
        });
        view.findViewById(R.id.value).setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                sortData.toggle(sortItem, true);
                notifyDataSetChanged();
                return true;
            }
        });

        return view;
    }
}
