$.fn.filepond.registerPlugin(FilePondPluginFileValidateSize);
$.fn.filepond.registerPlugin(FilePondPluginFileValidateType);

$.fn.filepond.setDefaults({
    maxFileSize: '5MB',
    acceptedFileTypes: [
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    ],
    fileValidateTypeLabelExpectedTypes: 'Only xls and xlsx files are supported'
});