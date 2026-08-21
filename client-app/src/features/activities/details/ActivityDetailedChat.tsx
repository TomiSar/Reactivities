import { Formik, Form, Field, FieldProps } from 'formik';
import { observer } from 'mobx-react-lite';
import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Segment, Header, Comment, Loader, Icon } from 'semantic-ui-react';
import { useStore } from '../../../app/stores/store';
import * as Yup from 'yup';
import { ChatComment } from '../../../app/models/comment';
import { formatDistanceToNow } from 'date-fns';

interface Props {
  activityId: string;
}

export default observer(function ActivityDetailedChat({ activityId }: Props) {
  const { commentStore } = useStore();

  useEffect(() => {
    if (activityId) {
      commentStore.createHubConnection(activityId);
    }
    return () => {
      commentStore.clearComments();
    };
  }, [commentStore, activityId]);

  return (
    <>
      <Segment
        textAlign='center'
        attached='top'
        inverted
        color='teal'
        style={{ border: 'none' }}
      >
        <Header>Chat about this event</Header>
      </Segment>
      <Segment attached clearing>
        <Formik
          onSubmit={(values, { resetForm }) =>
            commentStore.addComment(values).then(() => resetForm())
          }
          initialValues={{ body: '' }}
          validationSchema={Yup.object({
            body: Yup.string().required(),
          })}
        >
          {({ isSubmitting, isValid, handleSubmit }) => (
            <Form className='ui form'>
              <Field name='body'>
                {(props: FieldProps) => (
                  <div style={{ position: 'relative' }}>
                    <Loader active={isSubmitting} />
                    <textarea
                      placeholder='Enter your comment (Enter to submit, SHIFT + Enter for new line)'
                      rows={3}
                      {...props.field}
                      onKeyPress={(e) => {
                        if (e.key === 'Enter' && e.shiftKey) {
                          return;
                        }
                        if (e.key === 'Enter' && !e.shiftKey) {
                          e.preventDefault();
                          isValid && handleSubmit();
                        }
                      }}
                    />
                  </div>
                )}
              </Field>
            </Form>
          )}
        </Formik>
        <Comment.Group>
          {commentStore.comments.map((comment) => (
            <ChatCommentItem key={comment.id} comment={comment} />
          ))}
        </Comment.Group>
      </Segment>
    </>
  );
});

const ChatCommentItem = observer(function ChatCommentItem({
  comment,
}: {
  comment: ChatComment;
}) {
  const { commentStore, userStore } = useStore();
  const [isEditing, setIsEditing] = useState(false);
  const [editBody, setEditBody] = useState(comment.body);
  const isOwner = userStore.user?.username === comment.username;

  const renderTime = (dateInput: any) => {
    try {
      if (!dateInput) return null;

      const date = new Date(dateInput);

      if (isNaN(date.getTime())) return null;

      return formatDistanceToNow(date) + ' ago';
    } catch (e) {
      return null;
    }
  };

  const createdTime = renderTime(comment.createdAt) || 'some time ago';
  const updatedTime = renderTime(comment.updatedAt);

  return (
    <Comment>
      <Comment.Avatar src={comment.image || '/assets/user.png'} />
      <Comment.Content>
        {isOwner && (
          <div
            style={{
              float: 'right',
              display: 'flex',
              gap: '15px',
              marginTop: '2px',
            }}
          >
            <Icon
              name='edit'
              color='blue'
              size='large'
              style={{ cursor: 'pointer', marginRight: '5px' }}
              onClick={() => setIsEditing(!isEditing)}
            />
            <Icon
              name='trash'
              color='red'
              size='large'
              style={{ cursor: 'pointer' }}
              onClick={() => commentStore.deleteComment(comment.id)}
            />
          </div>
        )}
        <Comment.Author as={Link} to={`/profiles/${comment.username}`}>
          {comment.displayName}
        </Comment.Author>
        <Comment.Metadata>
          {updatedTime ? (
            <div
              style={{
                display: 'flex',
                flexDirection: 'column',
                gap: '2px',
                lineHeight: '1.2',
              }}
            >
              <span style={{ fontSize: '11px', color: 'rgba(0,0,0,.4)' }}>
                commented {createdTime}
              </span>
              <span
                style={{
                  fontSize: '11px',
                  color: 'orange',
                  fontStyle: 'italic',
                  fontWeight: 'bold',
                }}
              >
                updated comment {updatedTime}
              </span>
            </div>
          ) : (
            <div>commented {createdTime}</div>
          )}
        </Comment.Metadata>
        {isEditing ? (
          <div style={{ marginTop: '15px' }}>
            <textarea
              rows={3}
              value={editBody}
              onChange={(e) => setEditBody(e.target.value)}
              style={{ width: '100%', marginBottom: '5px', padding: '5px' }}
            />
            <button
              className='ui mini teal button'
              onClick={() => {
                commentStore.editComment(comment.id, editBody);
                setIsEditing(false);
              }}
            >
              Save
            </button>
            <button
              className='ui mini button'
              onClick={() => setIsEditing(false)}
            >
              Cancel
            </button>
          </div>
        ) : (
          <Comment.Text style={{ whiteSpace: 'pre-wrap' }}>
            {comment.body}
          </Comment.Text>
        )}
      </Comment.Content>
    </Comment>
  );
});
