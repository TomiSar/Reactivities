import { observer } from 'mobx-react-lite';
import { useEffect, useState } from 'react';
import { Button, Grid, Header } from 'semantic-ui-react';
import PhotoWidgetCropper from './PhotoWidgetCropper';
import PhotoUploadWidgetDropzone from './PhotoWidgetDropzone';

interface Props {
    loading: boolean;
    uploadPhoto: (file: Blob) => void;
}

export default observer(function PhotoUploadWidget({ loading, uploadPhoto }: Props) {
    const [files, setFiles] = useState<any>([]);
    const [cropper, setCropper] = useState<Cropper>();

    function onCrop() {
        if (cropper) {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            cropper.getCroppedCanvas().toBlob((blob) => uploadPhoto(blob!));
        }
    }

    useEffect(() => {
        return () => {
            files.forEach((file: any) => URL.revokeObjectURL(file.preview));
        };
    }, [files]);

    return (
        <>
            <Grid>
                <Grid.Row />
                <Grid.Column width={4}>
                    <Header color='teal' sub content='Step 1 - Add Photo' />
                    <PhotoUploadWidgetDropzone setFiles={setFiles} />
                </Grid.Column>
                <Grid.Column width={1} />
                <Grid.Column width={4}>
                    <Header sub color='teal' content='Step 2 - Resize image' />
                    {files && files.length > 0 && (
                        <PhotoWidgetCropper setCropper={setCropper} imagePreview={files[0].preview} />
                    )}
                </Grid.Column>
                <Grid.Column width={1} />
                <Grid.Column width={4}>
                    <Header sub color='teal' content='Step 3 - Preview & Upload' />
                    <div className='img-preview' style={{ minHeight: 200, overflow: 'hidden' }} />
                    {files && files.length > 0 && (
                        <>
                            <Button.Group widths={2}>
                                <Button loading={loading} onClick={onCrop} positive icon='check' />
                                <Button disabled={loading} onClick={() => setFiles([])} icon='close' />
                            </Button.Group>
                        </>
                    )}
                </Grid.Column>
            </Grid>
        </>
    );
});
