package neoplayer.neoremote;

import android.databinding.DataBindingUtil;
import android.text.Editable;
import android.text.TextWatcher;
import android.view.View;
import android.view.ViewGroup;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.BaseAdapter;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;

import neoplayer.neoremote.databinding.FindAdapterItemBinding;

public class FindAdapter extends BaseAdapter {
    private final MainActivity mainActivity;
    private ArrayList<FindData> findDataList;

    public FindAdapter(MainActivity mainActivity, ArrayList<FindData> findDataList) {
        super();
        this.mainActivity = mainActivity;
        this.findDataList = findDataList;
        sortList();
    }

    private void sortList() {
        Collections.sort(findDataList, new Comparator<FindData>() {
            @Override
            public int compare(FindData findData1, FindData findData2) {
                return findData1.tag.compareTo(findData2.tag);
            }
        });
    }

    @Override
    public int getCount() {
        return findDataList.size();
    }

    @Override
    public Object getItem(int i) {
        return findDataList.get(i);
    }

    @Override
    public long getItemId(int i) {
        return i;
    }

    boolean changing = false;

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        final FindData findData = (FindData) getItem(position);

        final FindAdapterItemBinding binding;
        if (convertView == null) {
            binding = DataBindingUtil.inflate(mainActivity.getLayoutInflater(), R.layout.find_adapter_item, parent, false);
            binding.getRoot().setTag(binding);
        } else
            binding = (FindAdapterItemBinding) convertView.getTag();

        binding.name.setText(findData.tag);

        changing = true;
        binding.type.setAdapter(new ArrayAdapter<>(mainActivity.getApplicationContext(), android.R.layout.simple_spinner_item, FindData.findTypes));
        for (int index = 0; index < FindData.findTypes.length; ++index)
            if (FindData.findTypes[index] == findData.findType)
                binding.type.setSelection(index);
        binding.value1.setText(findData.value1);
        binding.value2.setText(findData.value2);
        changing = false;

        binding.type.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> adapterView, View view, int pos, long id) {
                if (changing)
                    return;

                findData.findType = (FindData.FindType) adapterView.getItemAtPosition(pos);
                binding.value1.setVisibility(findData.findType.paramCount >= 1 ? View.VISIBLE : View.GONE);
                binding.value2.setVisibility(findData.findType.paramCount >= 2 ? View.VISIBLE : View.GONE);
            }

            @Override
            public void onNothingSelected(AdapterView<?> adapterView) {
            }
        });

        binding.value1.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void afterTextChanged(Editable editable) {
                if (!changing)
                    findData.value1 = editable.toString().trim().toLowerCase();
            }
        });

        binding.value2.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void afterTextChanged(Editable editable) {
                if (!changing)
                    findData.value2 = editable.toString().trim().toLowerCase();
            }
        });

        return binding.getRoot();
    }
}
