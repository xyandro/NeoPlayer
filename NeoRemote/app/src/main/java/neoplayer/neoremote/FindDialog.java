package neoplayer.neoremote;

import android.app.Dialog;
import android.app.DialogFragment;
import android.databinding.DataBindingUtil;
import android.os.Bundle;
import android.support.annotation.Nullable;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;

import java.util.ArrayList;
import java.util.Iterator;

import neoplayer.neoremote.databinding.FindDialogBinding;

public class FindDialog extends DialogFragment {
    private MainActivity mainActivity;
    private FindDialogBinding binding;
    private FindAdapter findAdapter;
    private ArrayList<FindData> findDataList;

    @Override
    public Dialog onCreateDialog(Bundle savedInstanceState) {
        Dialog dialog = super.onCreateDialog(savedInstanceState);
        dialog.getWindow().requestFeature(Window.FEATURE_NO_TITLE);
        return dialog;
    }

    @Nullable
    @Override
    public View onCreateView(LayoutInflater inflater, @Nullable ViewGroup container, Bundle savedInstanceState) {
        binding = DataBindingUtil.inflate(inflater, R.layout.find_dialog, container, false);

        binding.clear.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                for (FindData findData : findDataList)
                    findData.findType = FindData.None;
                findAdapter.notifyDataSetChanged();
            }
        });
        binding.submit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                Iterator<FindData> findDataIterator = findDataList.iterator();
                while (findDataIterator.hasNext()) {
                    FindData findData = findDataIterator.next();
                    if (findData.findType == FindData.None)
                        findDataIterator.remove();
                }
                if (findDataList.isEmpty())
                    findDataList = null;

                mainActivity.setFindDataList(findDataList);
                dismiss();
            }
        });
        findAdapter = new FindAdapter(mainActivity, findDataList);
        binding.list.setAdapter(findAdapter);

        return binding.getRoot();
    }

    public static FindDialog createDialog(MainActivity mainActivity, ArrayList<FindData> findDataList) {
        FindDialog findDialog = new FindDialog();
        findDialog.mainActivity = mainActivity;
        findDialog.findDataList = findDataList;
        return findDialog;
    }
}