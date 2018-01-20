package neoplayer.neoremote;

import android.databinding.DataBindingUtil;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;

import neoplayer.neoremote.databinding.SortAdapterItemBinding;

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
        return sortData.size();
    }

    @Override
    public Object getItem(int i) {
        return sortData.getAlphaOrder(i);
    }

    @Override
    public long getItemId(int i) {
        return i;
    }

    private int getSortResource(SortData.SortDirection sortDirection) {
        switch (sortDirection) {
            case Ascending:
                return R.drawable.sortascending;
            case Descending:
                return R.drawable.sortdescending;
            default:
                return R.drawable.sortnone;
        }
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        final SortData.SortItem sortItem = (SortData.SortItem) getItem(position);

        SortAdapterItemBinding binding = DataBindingUtil.inflate(mainActivity.getLayoutInflater(), R.layout.sort_adapter_item, parent, false);

        binding.item.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                sortData.toggle(sortItem, false);
                notifyDataSetChanged();
            }
        });
        binding.item.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                sortData.toggle(sortItem, true);
                notifyDataSetChanged();
                return true;
            }
        });

        binding.direction.setImageResource(getSortResource(sortItem.direction));
        if (sortItem.priority != null)
            binding.priority.setText(sortItem.priority.toString());
        binding.tag.setText(sortItem.tag);

        return binding.getRoot();
    }
}
