import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { makeAutoObservable, runInAction } from 'mobx';
import { ChatComment } from '../models/comment';
import { store } from './store';
import { showErrorToast } from '../../utils/helpers';

export default class CommentStore {
    comments: ChatComment[] = [];
    hubConnection: HubConnection | null = null;

    constructor() {
        makeAutoObservable(this);
    }

    createHubConnection = (activityId: string) => {
        if (store.activityStore.selectedActivity) {
            this.hubConnection = new HubConnectionBuilder()
                .withUrl(process.env.REACT_APP_CHAT_URL + '?activityId=' + activityId, {
                    accessTokenFactory: () => store.userStore.user?.token!,
                })
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Information)
                .build();

            this.hubConnection.start().catch((error) => console.log('Error establishing connection: ', error));

            this.hubConnection.on('LoadComments', (comments: ChatComment[]) => {
                runInAction(() => {
                    comments.forEach((comment) => {
                        comment.createdAt = new Date(comment.createdAt);
                    });
                    this.comments = comments;
                });
            });

            this.hubConnection.on('ReceiveComment', (comment) => {
                runInAction(() => {
                    comment.createdAt = new Date(comment.createdAt);
                    this.comments.unshift(comment);
                });
            });

            this.hubConnection.on('EditComment', (editedComment: ChatComment) => {
                runInAction(() => {
                    editedComment.createdAt = new Date(editedComment.createdAt);

                    if (editedComment.updatedAt) {
                        editedComment.updatedAt = new Date(editedComment.updatedAt);
                    }

                    const index = this.comments.findIndex((x) => x.id === editedComment.id);
                    if (index !== -1) {
                        this.comments[index] = editedComment;
                    }
                });
            });

            this.hubConnection.on('DeleteComment', (id: number) => {
                runInAction(() => {
                    this.comments = this.comments.filter((x) => x.id !== id);
                });
            });
        }
    };

    stopHubConnection = () => {
        this.hubConnection?.stop().catch((error) => console.log('Error stopping connection: ', error));
    };

    clearComments = () => {
        this.comments = [];
        this.stopHubConnection();
    };

    addComment = async (values: any) => {
        values.activityId = store.activityStore.selectedActivity?.id;
        try {
            await this.hubConnection?.invoke('SendComment', values);
        } catch (error) {
            console.log(error);
            showErrorToast('Send', error);
        }
    };

    editComment = async (id: number, body: string) => {
        try {
            await this.hubConnection?.invoke('EditComment', { id, body });
        } catch (error) {
            console.log(error);
            showErrorToast('Edit', error);
        }
    };

    deleteComment = async (id: number) => {
        const originalComments = [...this.comments];

        try {
            runInAction(() => {
                this.comments = this.comments.filter((x) => x.id !== id);
            });

            await this.hubConnection?.invoke('DeleteComment', { id });
        } catch (error) {
            console.log(error);
            runInAction(() => {
                this.comments = originalComments;
            });
            showErrorToast('Delete', error);
        }
    };
}
